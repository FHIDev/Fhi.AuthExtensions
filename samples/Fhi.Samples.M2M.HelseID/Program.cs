using Duende.AccessTokenManagement;
using Fhi.Authentication;
using Fhi.Authentication.ClientCredentials;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace M2M.Host.HelseID;

/// <summary>
/// Example showing either JWK or certificate-based authentication.
/// Both options are demonstrated in this sample.
/// Certificate-based authentication uses ICertificateJwkResolver for explicit control.
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
            
            // Toggle between JWK-based or certificate-based authentication, implement method as required
            var useCertificate = false;

            if (useCertificate)
            {
                ConfigureCertificateAuthentication(services, apiSection);
            }
            else
            {
                ConfigureJwkAuthentication(services, apiSection);
            }
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
}