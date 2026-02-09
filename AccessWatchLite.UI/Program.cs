using Azure.Identity;
using AccessWatchLite.UI.Components;
using AccessWatchLite.Infrastructure;
using Microsoft.Extensions.Azure;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;

var builder = WebApplication.CreateBuilder(args);

// Key Vault
builder.Configuration.AddAzureKeyVault(
    new Uri("https://kv-tfm-security.vault.azure.net/"),
    new DefaultAzureCredential());

// ===================================================
// AUTENTICACIÓN CON MICROSOFT ENTRA ID (Azure AD)
// ===================================================
builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(options =>
    {
        builder.Configuration.Bind("AzureAd", options);
        options.SignInScheme = "Cookies";
        
        // Configuración de eventos para manejar logout
        options.Events = new OpenIdConnectEvents
        {
            OnRedirectToIdentityProviderForSignOut = context =>
            {
                // Redirigir a la página de login después del logout
                context.Properties.RedirectUri = "/";
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    // Requiere autenticación para todas las páginas por defecto
    options.FallbackPolicy = options.DefaultPolicy;
});

builder.Services.AddControllersWithViews()
    .AddMicrosoftIdentityUI();

// Blazor Web App (.NET 8)
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// HttpClient para servicios
builder.Services.AddHttpClient();

// Infrastructure services (repositories, detection engine, alert service)
builder.Services.AddInfrastructure();

// UI Services
builder.Services.AddScoped<AccessWatchLite.UI.Services.CsvImportService>();
builder.Services.AddAzureClients(clientBuilder =>
{
    clientBuilder.AddBlobServiceClient(builder.Configuration["StorageConnection:blobServiceUri"]!).WithName("StorageConnection");
    clientBuilder.AddQueueServiceClient(builder.Configuration["StorageConnection:queueServiceUri"]!).WithName("StorageConnection");
    clientBuilder.AddTableServiceClient(builder.Configuration["StorageConnection:tableServiceUri"]!).WithName("StorageConnection");
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// ===================================================
// MIDDLEWARE DE AUTENTICACIÓN (orden importante)
// ===================================================
app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

// Endpoints Razor Components
app.MapRazorComponents<App>()
   .AddInteractiveServerRenderMode();

// Endpoints de autenticación Microsoft Identity
app.MapControllers();

app.Run();
