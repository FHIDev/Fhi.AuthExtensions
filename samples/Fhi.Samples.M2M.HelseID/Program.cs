using Duende.AccessTokenManagement;
using Fhi.Authentication;
using Fhi.Authentication.ClientCredentials;
using Fhi.Authentication.Tokens;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace M2M.Host.HelseID;

/// <summary>
/// Example showing three different ways to configure client credentials authentication:
/// 1. Direct JWK configuration (simple, resolved at startup)
/// 2. Direct certificate configuration (explicit control, resolved at startup)
/// 3. ISecretStore pattern (RECOMMENDED - runtime resolution with DI)
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
            // Option 3: ISecretStore (RECOMMENDED - flexible, testable, DI-friendly)
            
            ConfigureWithSecretStore(services, apiSection);
            
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
        
        // Certificate will be resolved at runtime using ICertificateKeyHandler
        // No need to resolve at configuration time when using certificate-based options
        
        services
            .AddClientCredentialsClientOptions(
                api.ClientName,
                api.Authentication.Authority,
                api.Authentication.ClientId,
                api.Authentication.Certificate,  // Pass certificate options directly
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
    /// Option 3: Configure authentication using ISecretStore pattern (RECOMMENDED).
    /// This approach allows you to register different ISecretStore implementations in DI
    /// based on your environment or configuration needs.
    ///
    /// Example implementations:
    /// - FileSecretStore: For JWK from configuration/environment variables
    /// - CertificateSecretStore: For certificates from Windows certificate store
    /// </summary>
    /// 
    private static void ConfigureWithSecretStore(IServiceCollection services, IConfigurationSection apiSection)
    {
        services
            .AddOptions<HelseIdProtectedApiOption>()
            .Bind(apiSection)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var api = apiSection.Get<HelseIdProtectedApiOption>() ?? new HelseIdProtectedApiOption();

        // Register the appropriate ISecretStore implementation based on configuration
        var certificateThumbprint = apiSection.GetValue<string>("Authentication:Certificate:Thumbprint");
        
        if (!string.IsNullOrEmpty(certificateThumbprint))
        {
            services.AddSingleton<CertificateSecretManager>();
            
            services.AddSingleton<ISecretStore>(sp =>
            {
                var certificateOptions = new CertificateOptions
                {
                    Thumbprint = certificateThumbprint,
                    StoreLocation = CertificateStoreLocation.CurrentUser
                };
                
                return new CertificateSecretStore(
                    certificateOptions,
                    sp.GetRequiredService<IPrivateKeyHandler>(),
                    sp.GetRequiredService<ILogger<CertificateSecretStore>>(),
                    sp.GetRequiredService<CertificateSecretManager>()); 
            });
        }
        else if (!string.IsNullOrEmpty(api.Authentication.PrivateJwk))
        {
            services.AddSingleton<ISecretStore>(sp =>
                new FileSecretStore(
                    api.Authentication.PrivateJwk,
                    sp.GetRequiredService<ILogger<FileSecretStore>>()));
        }

        // Configure client credentials using the ISecretStore
        services
            .AddClientCredentialsClientOptions(
                api.ClientName,
                api.Authentication.Authority,
                api.Authentication.ClientId,
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