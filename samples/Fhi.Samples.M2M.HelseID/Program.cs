using Duende.AccessTokenManagement;
using Fhi.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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

            ConfigureJwkAuthentication(services, apiSection);
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

        var api = apiSection.Get<HelseIdProtectedApiOption>()!;

        // Simple JWK-based authentication
        var builder = services
            .AddClientCredentialsClientOptions(
                HelseIdProtectedApiOption.ClientName,
                api.Authentication.Authority,
                api.Authentication.ClientId,
                PrivateJwk.ParseFromJson(api.Authentication.PrivateJwk),
                api.Authentication.Scope);

        // Optional: Configure custom client assertion expiration (default is 10 seconds)
        builder.ClientAssertionOptions!.Configure(options => options.ExpirationSeconds = 30);

        builder.AddClientCredentialsHttpClient(client => { client.BaseAddress = new Uri(api.BaseAddress); });

        // Add token management services
        services.AddDistributedMemoryCache();
        services.AddClientCredentialsTokenManagement();
        services.AddInMemoryDiscoveryService([api.Authentication.Authority]);

    }


}