namespace AccessWatchLite.Domain;

/// <summary>
/// Mapeo persistente de nombres reales a pseudónimos
/// </summary>
public sealed class NameMapping
{
    public Guid Id { get; set; }
    public string OriginalHash { get; set; } = string.Empty; // SHA256 del nombre original
    public string PseudonymFirstName { get; set; } = string.Empty;
    public string PseudonymLastName { get; set; } = string.Empty;
    public string PseudonymFullName { get; set; } = string.Empty;
    public string PseudonymEmail { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
