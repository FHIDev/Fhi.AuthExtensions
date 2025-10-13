using Duende.AccessTokenManagement;
using Fhi.Authentication;
using Fhi.Authentication.ClientCredentials;
using Fhi.Authentication.OpenIdConnect;
using Fhi.Samples.WorkerServiceMultipleClients.Refit;
using Refit;

public partial class Program
{
    public static IHostBuilder CreateHostBuilderRefit(string[] args) =>
       Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((hostingContext, config) =>
            {
                config.AddJsonFile("appsettings.Refit.json", optional: false, reloadOnChange: true);
            })
           .ConfigureServices((context, services) =>
           {
               IConfiguration configuration = context.Configuration;

               services.AddClientCredentialsTokenManagement();
               services.AddDistributedMemoryCache();
               services.AddTransient<IClientAssertionService, ClientCredentialsAssertionService>();
               services.AddHostedService<WorkerRefit>();

               var apiSection = configuration.GetSection("Api");
               services.AddOptions<ApiOption>()
                .Bind(apiSection)
                .ValidateDataAnnotations()
                .ValidateOnStart();



               var apiOption = apiSection.Get<ApiOption>() ?? new ApiOption();

               services
                 .AddOptions<ClientAssertionOptions>(apiOption.ClientName)
                 .Configure<IDiscoveryDocumentStore>((options, discoveryStore) =>
                 {
                     var discoveryDocument = discoveryStore.Get(apiOption.ClientAuthentication.Authority);
                     options.Issuer = discoveryDocument?.Issuer ?? string.Empty;
                     options.PrivateJwk = apiOption.ClientAuthentication.Secret;
                 });

               services
                   .AddOptions<ClientCredentialsClient>("TokenClientName")
                   .Configure<IDiscoveryDocumentStore>((options, discoveryStore) =>
                   {
                       var discoveryDocument = discoveryStore.Get(apiOption.ClientAuthentication.Authority);
                       options.TokenEndpoint = discoveryDocument?.TokenEndpoint is not null ? new Uri(discoveryDocument.TokenEndpoint) : null;
                       options.ClientId = ClientId.Parse(apiOption.ClientAuthentication.ClientId);
                       options.Scope = Scope.Parse(apiOption.ClientAuthentication.Scope);
                   })
                   .Validate(clientCredential =>
                   !string.IsNullOrWhiteSpace(clientCredential.ClientId)
                   && !string.IsNullOrWhiteSpace(clientCredential?.TokenEndpoint?.AbsoluteUri),
                   failureMessage: "ClientId, and TokenEndpoint must be provided and not empty.");

               services.AddClientCredentialsHttpClient(apiOption!.ClientName, ClientCredentialsClientName.Parse("TokenClientName"), client =>
               {
                   client.BaseAddress = new Uri(apiOption?.BaseAddress!);
               })
               .AddTypedClient(RestService.For<IHealthRecordApi>);

               services.AddInMemoryDiscoveryService([apiOption.ClientAuthentication.Authority]);
           });
}

