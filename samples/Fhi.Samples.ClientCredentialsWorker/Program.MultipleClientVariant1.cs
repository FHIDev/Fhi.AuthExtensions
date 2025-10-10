using Duende.AccessTokenManagement;
using Fhi.Authentication;
using Fhi.Authentication.ClientCredentials;
using Fhi.Authentication.OpenIdConnect;
using Fhi.Samples.WorkerServiceMultipleClients.MultipleClientVariant1;
using Fhi.Samples.WorkerServiceMultipleClients.MultipleClientVariant1.Configurations;

/// <summary>
/// 
/// </summary>
public partial class Program
{
    public static IHostBuilder CreateHostBuilderMultipleClientVariant1(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((hostingContext, config) =>
            {
                config.AddJsonFile("appsettings.MultipleClientVariant1.json", optional: false, reloadOnChange: true);
            })
            .ConfigureServices((context, services) =>
            {
                IConfiguration configuration = context.Configuration;
                /**********************************************************************
                 *  1. Register the core services needed for OAuth client credentials flow and token management
                 *  -  AddClientCredentialsTokenManagement(): Enables automatic token acquisition and refresh
                 *  -  AddDistributedMemoryCache(): Provides token caching capabilities
                 *  -  Worker: The background service that will consume the APIs
                 **********************************************************************/

                services.AddClientCredentialsTokenManagement();
                services.AddDistributedMemoryCache();
                services.AddTransient<IClientAssertionService, ClientCredentialsAssertionService>();
                services.AddHostedService<WorkerMultipleClientVariant1>();
                /*****************************************************************************
                 * 2. Configure Oidc clients that should be used by the HttpClient for authentication
                 * ****************************************************************************/

                var oidcClientSection = configuration.GetSection("OidcClients");
                // Register available authorities to get discovery documents
                services.AddInMemoryDiscoveryService(oidcClientSection.GetChildren().Select(c => c.Get<OidcClientOption>()?.Authority ?? ""));
                foreach (var clientSection in oidcClientSection.GetChildren())
                {
                    var clientOption = clientSection.Get<OidcClientOption>() ?? new OidcClientOption();

                    if (clientOption.SecretType == "JWK")
                    {
                        services
                            .AddOptions<ClientAssertionOptions>(clientSection.Key)
                            .Configure<IDiscoveryDocumentStore>((options, discoveryStore) =>
                            {
                                var discoveryDocument = discoveryStore.Get(clientOption.Authority);
                                options.Issuer = discoveryDocument?.Issuer ?? string.Empty;
                                options.PrivateJwk = clientOption.Secret;
                            });
                    }


                    services
                    .AddOptions<ClientCredentialsClient>(clientSection.Key)
                    .Configure<IDiscoveryDocumentStore>((options, discoveryStore) =>
                    {
                        var discoveryDocument = discoveryStore.Get(clientOption!.Authority);
                        options.TokenEndpoint = discoveryDocument?.TokenEndpoint is not null ? new Uri(discoveryDocument.TokenEndpoint) : null;
                        options.ClientId = ClientId.Parse(clientOption.ClientId);
                        options.Scope = Scope.Parse(clientOption.Scope);
                        if (clientOption.SecretType == "SharedSecret")
                            options.ClientSecret = ClientSecret.Parse(clientOption.Secret);
                    })
                    .Validate(clientCredential =>
                        !string.IsNullOrWhiteSpace(clientCredential.ClientId)
                        && !string.IsNullOrWhiteSpace(clientCredential?.TokenEndpoint?.AbsoluteUri)
                        && !string.IsNullOrWhiteSpace(clientCredential?.Parameters.FirstOrDefault(x => x.Key == ClientCredentialParameter.Issuer).Value),
                        failureMessage: "ClientId, ClientSecret, and TokenEndpoint must be provided and not empty.");
                }

                /*****************************************************************************
                 * 3. Register HttpClients for the APIs to be consumed
                 * ****************************************************************************/
                // Register the HttpClient for API 1
                var api1Section = configuration.GetSection("Apis:ApiClientSample1");
                services
                    .AddOptions<ApiClientSample1>()
                    .Bind(api1Section)
                    .ValidateDataAnnotations()
                    .ValidateOnStart();
                var api1Config = api1Section.Get<ApiClientSample1>();
                services.AddClientCredentialsHttpClient(api1Config!.ClientName, ClientCredentialsClientName.Parse(api1Config!.OidcClientName), (sp, client) =>
                {
                    client.BaseAddress = new Uri(api1Config.BaseAddress!);
                });

                // Register the HttpClient for API 2 and connect to Oidc client configurations
                var api2Section = configuration.GetSection("Apis:ApiClientSample2");
                services.AddOptions<ApiClientSample2>()
                    .Bind(api2Section)
                    .ValidateDataAnnotations()
                    .ValidateOnStart();

                var api2Config = api2Section.Get<ApiClientSample2>();
                services.AddClientCredentialsHttpClient(api2Config!.ClientName, ClientCredentialsClientName.Parse(api2Config.OidcClientName), (sp, client) =>
                {
                    client.BaseAddress = new Uri(api2Config.BaseAddress!);
                });
            });
}
