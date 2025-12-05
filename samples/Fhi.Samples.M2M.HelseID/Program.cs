using Duende.AccessTokenManagement;
using Fhi.Authentication;
using Fhi.Authentication.ClientCredentials;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace M2M.Host.HelseID;

/// <summary>
/// Example showing three different ways to configure client credentials authentication:
/// 1. Direct JWK configuration
/// 2. Direct certificate configuration with explicit resolver
/// 3. Factory pattern with automatic secret type detection (RECOMMENDED)
/// </summary>
public partial class Program
{
    public static async Task Main(string[] args)
    {
        var builder = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder(args);
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
        });
        builder.ConfigureServices((context, services) =>
        {
            services.AddTransient<HealthRecordService>();

            var apiSection = context.Configuration.GetSection("HelseIdProtectedApi");
            
            // Choose configuration approach:
            // Option 1: Direct JWK (simple, good for dev with user secrets)
            // Option 2: Direct certificate (explicit control, more verbose)
            // Option 3: Factory pattern (RECOMMENDED - config-driven, no manual flags)
            
            ConfigureWithSecretStoreFactory(services, apiSection);
            
            // Alternative options (uncomment to try):
            // ConfigureJwkAuthentication(services, apiSection);
            // ConfigureCertificateAuthentication(services, apiSection);
        });

        var app = builder.Build();
        await app.StartAsync();
    }

    /// <summary>
    /// Option 1: Configure JWK-based authentication
    /// </summary>
    private static void ConfigureJwkAuthentication(IServiceCollection services,
        IConfigurationSection apiSection)
    {
        services
            .AddOptions<HelseIdProtectedApiOption>()
            .Bind(apiSection)
            .ValidateDataAnnotations()
            .ValidateOnStart();
        
        var api = apiSection.Get<HelseIdProtectedApiOption>() ?? new HelseIdProtectedApiOption();

        // Simple JWK-based authentication
        services
            .AddClientCredentialsClientOptions(
                api.ClientName,
                api.Authentication.Authority,
                api.Authentication.ClientId,
                PrivateJwk.ParseFromJson(api.Authentication.PrivateJwk),
                api.Authentication.Scope)
            .AddClientCredentialsHttpClient(client => { client.BaseAddress = new Uri(api.BaseAddress); });

        // Add token management services
        services.AddDistributedMemoryCache();
        services.AddClientCredentialsTokenManagement();
        services.AddInMemoryDiscoveryService([api.Authentication.Authority]);
        
    }

    /// <summary>
    /// Option 2: Configure certificate-based authentication
    /// Demonstrates explicit control over certificate retrieval and JWK conversion.
    /// </summary>
    private static void ConfigureCertificateAuthentication(IServiceCollection services, IConfigurationSection apiSection)
    {
        services
            .AddOptions<HelseIdCertificateApiOption>()
            .Bind(apiSection)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var api = apiSection.Get<HelseIdCertificateApiOption>() ?? new HelseIdCertificateApiOption();
        
        // Create resolver directly for configuration-time use
        var resolver = new CertificateJwkResolver();
        
        // Convert certificate configuration (thumbprint or PEM) to JWK
        var privateJwkJson = resolver.ResolveToJwk(api.Authentication.Certificate);
        
        services
            .AddClientCredentialsClientOptions(
                api.ClientName,
                api.Authentication.Authority,
                api.Authentication.ClientId,
                PrivateJwk.ParseFromJson(privateJwkJson),
                api.Authentication.Scope)
            .AddClientCredentialsHttpClient(client =>
            {
                client.BaseAddress = new Uri(api.BaseAddress);
            });
        
        services.AddDistributedMemoryCache();
        services.AddClientCredentialsTokenManagement();
        services.AddInMemoryDiscoveryService([api.Authentication.Authority]);
    }

    /// <summary>
    /// Option 3: Configure authentication using the SecretStoreFactory pattern (RECOMMENDED).
    /// The secret type (certificate or file) is automatically detected from configuration.
    /// No manual flags needed - just populate what's in your appsettings.json!
    /// </summary>
    /// <remarks>
    /// Auto-detection logic:
    /// - If CertificateThumbprint is present → uses CertificateSecretStore
    /// - If PrivateJwk is present → uses FileSecretStore
    /// This approach eliminates the need for manual configuration switches and makes
    /// it easy to switch between dev (PrivateJwk) and prod (Certificate) environments.
    /// </remarks>
    private static void ConfigureWithSecretStoreFactory(IServiceCollection services, IConfigurationSection apiSection)
    {
        services
            .AddOptions<HelseIdProtectedApiOption>()
            .Bind(apiSection)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var api = apiSection.Get<HelseIdProtectedApiOption>() ?? new HelseIdProtectedApiOption();

        // Use the factory-based extension that auto-detects secret type from configuration
        services
            .AddClientCredentialsClientOptionsWithSecretStore(
                api.ClientName,
                api.Authentication.Authority,
                api.Authentication.ClientId,
                secretStore =>
                {
                    // Just populate from config - system auto-detects which to use!
                    // For file-based secrets (dev/testing):
                    secretStore.PrivateJwk = api.Authentication.PrivateJwk;
                    
                    // For certificate-based secrets (production):
                    secretStore.ClientId = api.Authentication.ClientId;
                    // Get certificate thumbprint from config if it exists
                    secretStore.CertificateThumbprint = apiSection.GetValue<string>("Authentication:Certificate:Thumbprint");
                    
                    // The factory will automatically choose based on which is populated
                },
                api.Authentication.Scope)
            .AddClientCredentialsHttpClient(client =>
            {
                client.BaseAddress = new Uri(api.BaseAddress);
            });

        // Add token management services
        services.AddDistributedMemoryCache();
        services.AddClientCredentialsTokenManagement();
        services.AddInMemoryDiscoveryService([api.Authentication.Authority]);
    }
}