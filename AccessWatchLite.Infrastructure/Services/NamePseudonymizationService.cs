using System.Security.Cryptography;
using System.Text;
using AccessWatchLite.Application.Services;
using AccessWatchLite.Application.Sql;
using AccessWatchLite.Domain;

namespace AccessWatchLite.Infrastructure.Services;

public sealed class NamePseudonymizationService : INamePseudonymizationService
{
    private readonly INameMappingRepository _repository;
    
    // Listas de nombres ficticios para generar pseudónimos realistas
    private static readonly string[] FirstNames = 
    {
        "Jaime", "Sofia", "Carlos", "Laura", "Miguel", "Ana", "David", "Elena",
        "Pablo", "Maria", "Javier", "Carmen", "Antonio", "Isabel", "Luis", "Raquel",
        "Jorge", "Beatriz", "Fernando", "Cristina", "Alberto", "Patricia", "Sergio", "Monica",
        "Daniel", "Natalia", "Manuel", "Rosa", "Pedro", "Silvia", "Adrian", "Marta",
        "Alejandro", "Lucia", "Francisco", "Teresa", "Jose", "Pilar", "Rafael", "Gloria",
        "Andres", "Dolores", "Victor", "Amparo", "Ricardo", "Inmaculada", "Diego", "Josefa",
        "Roberto", "Francisca", "Angel", "Mercedes", "Raul", "Julia", "Oscar", "Antonia"
    };

    private static readonly string[] LastNames = 
    {
        "Garcia", "Rodriguez", "Martinez", "Lopez", "Gonzalez", "Fernandez", "Sanchez", "Perez",
        "Martin", "Gomez", "Jimenez", "Ruiz", "Hernandez", "Diaz", "Moreno", "Muñoz",
        "Alvarez", "Romero", "Gutierrez", "Alonso", "Navarro", "Torres", "Dominguez", "Gil",
        "Vazquez", "Ramos", "Serrano", "Blanco", "Castro", "Suarez", "Ortega", "Delgado",
        "Molina", "Morales", "Ortiz", "Iglesias", "Gimenez", "Santos", "Castillo", "Rubio",
        "Sanz", "Mendez", "Cruz", "Prieto", "Flores", "Herrera", "Aguilar", "Guerrero"
    };

    private static readonly string[] Adjectives = 
    {
        "Guapo", "Valiente", "Inteligente", "Rapido", "Fuerte", "Brillante", "Audaz", "Listo",
        "Sabio", "Noble", "Veloz", "Agil", "Astuto", "Ingenioso", "Habil", "Diestro"
    };

    private readonly Random _random = new();
    private readonly SemaphoreSlim _lock = new(1, 1);

    public NamePseudonymizationService(INameMappingRepository repository)
    {
        _repository = repository;
    }

    public async Task<(string fullName, string email)> GetPseudonymAsync(string originalName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(originalName))
        {
            return ("Usuario Anónimo", "anonimo@ejemplo.com");
        }

        // Generar hash del nombre original
        var hash = ComputeHash(originalName);

        // Buscar mapeo existente
        var existing = await _repository.GetByOriginalHashAsync(hash, cancellationToken);
        if (existing != null)
        {
            return (existing.PseudonymFullName, existing.PseudonymEmail);
        }

        // Generar nuevo pseudónimo
        await _lock.WaitAsync(cancellationToken);
        try
        {
            // Double-check después de obtener el lock
            existing = await _repository.GetByOriginalHashAsync(hash, cancellationToken);
            if (existing != null)
            {
                return (existing.PseudonymFullName, existing.PseudonymEmail);
            }

            // Generar pseudónimo basado en el hash (determinístico)
            var seed = Math.Abs(hash.GetHashCode());
            var rng = new Random(seed);
            
            var firstName = FirstNames[rng.Next(FirstNames.Length)];
            var lastName = LastNames[rng.Next(LastNames.Length)];
            var adjective = Adjectives[rng.Next(Adjectives.Length)];
            
            // Hacer el nombre único agregando adjetivo o número si ya existe
            var fullName = $"{firstName} {lastName}";
            var count = await _repository.GetCountAsync(cancellationToken);
            
            // Si hay muchos usuarios, agregar adjetivo para variedad
            if (count > 50 && rng.Next(100) < 30)
            {
                fullName = $"{firstName} {adjective}";
            }
            
            var email = $"{firstName.ToLower()}.{lastName.ToLower()}@ejemplo.com";

            // Guardar mapeo
            var mapping = new NameMapping
            {
                Id = Guid.NewGuid(),
                OriginalHash = hash,
                PseudonymFirstName = firstName,
                PseudonymLastName = lastName,
                PseudonymFullName = fullName,
                PseudonymEmail = email,
                CreatedAt = DateTime.UtcNow
            };

            await _repository.InsertAsync(mapping, cancellationToken);

            return (fullName, email);
        }
        finally
        {
            _lock.Release();
        }
    }

    private static string ComputeHash(string value)
    {
        var salt = "AccessWatchLite_Names";
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes($"{salt}:{value}"));
        return Convert.ToHexString(bytes);
    }
}
