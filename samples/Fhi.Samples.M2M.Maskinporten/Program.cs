using Altinn.ApiClients.Maskinporten.Config;
using Altinn.ApiClients.Maskinporten.Extensions;
using Altinn.ApiClients.Maskinporten.Services;
using M2M.Host.Maskinporten;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

public partial class Program
{
    public static async Task Main(string[] args)
    {
        var builder = Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((hostingContext, config) =>
            {
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                config.AddUserSecrets<Program>();
            })
            .ConfigureServices((context, services) =>
            {
                services.AddTransient<HealthRecordService>();

                var maskinportenProtectedApiSection = context.Configuration.GetSection("MaskinPortenProtectedApi");
                services
                    .AddOptions<MaskinPortenProtectedApiOption>()
                    .Bind(maskinportenProtectedApiSection)
                    .ValidateDataAnnotations()
                    .ValidateOnStart();

                var maskinPortenProtectedApi = maskinportenProtectedApiSection.Get<MaskinPortenProtectedApiOption>();
                services.AddMaskinportenHttpClient<SettingsJwkClientDefinition>(
                    maskinPortenProtectedApi?.ClientName!,
                    new MaskinportenSettings()
                    {
                        ClientId = maskinPortenProtectedApi!.Authentication.ClientId,
                        Scope = maskinPortenProtectedApi.Authentication.Scope,
                        EncodedJwk = maskinPortenProtectedApi.Authentication.PrivateJwk,
                        Environment = maskinPortenProtectedApi.Authentication.Environment,
                        Resource = maskinPortenProtectedApi.Authentication.Resource
                    });
            });

        var app = builder.Build();

        await app.StartAsync();
        var response = await app.Services.GetRequiredService<HealthRecordService>().GetHealthRecords();
        await app.StopAsync();
    }
}

