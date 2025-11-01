using Duende.AccessTokenManagement;
using Fhi.Authentication;
using M2M.Host.HelseID;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

public partial class Program
{
    public static async Task Main(string[] args)
    {
        var builder = Host.CreateDefaultBuilder(args);
        builder.ConfigureAppConfiguration((hostingContext, config) =>
        {
            config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
        });
        builder.ConfigureServices((context, services) =>
        {
            services.AddTransient<HealthRecordService>();

            var apiSection = context.Configuration.GetSection("HelseIdProtectedApi");
            services
                    .AddOptions<HelseIdProtectedApiOption>()
                    .Bind(apiSection)
                    .ValidateDataAnnotations()
                    .ValidateOnStart();

            var api = apiSection.Get<HelseIdProtectedApiOption>() ?? new HelseIdProtectedApiOption();
            var clientCredentialsOption = services
                .AddClientCredentialsClientOptions(
                    api.ClientName,
                    api.Authentication.Authority,
                    api.Authentication.ClientId,
                    PrivateJwk.ParseFromJson(api.Authentication.PrivateJwk),
                    api.Authentication.Scope)
                ////DPoPProofKey.ParseOrDefault(api.Authentication.PrivateJwk))
                .AddClientCredentialsHttpClient(client =>
                {
                    client.BaseAddress = new Uri(api?.BaseAddress!);
                });
            // Using Refit to create the typed client instead of a regular HttpClient
            ////  .AddTypedClient(RestService.For<IHealthRecordApi>);

            //Add token management services. Must be added after
            services.AddDistributedMemoryCache();
            services.AddClientCredentialsTokenManagement();
            services.AddInMemoryDiscoveryService([api.Authentication.Authority]);
        });
        var app = builder.Build();

        await app.StartAsync();
        ////var refitResponse = await app.Services.GetRequiredService<IHealthRecordApi>().GetHealthRecordsAsync();
        var response = await app.Services.GetRequiredService<HealthRecordService>().GetHealthRecords();
        await app.StopAsync();
    }
}

