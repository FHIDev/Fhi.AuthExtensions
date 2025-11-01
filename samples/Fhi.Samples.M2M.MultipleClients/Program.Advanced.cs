using Client.ClientCredentialsWorkers.MultipleHttpClients.Options;
using Duende.AccessTokenManagement;
using Fhi.Authentication;
using Fhi.Authentication.ClientCredentials;
using Fhi.Authentication.OpenIdConnect;
using Fhi.Samples.WorkerServiceMultipleClients.MultipleClientVariant2;

public partial class Program
{
    public static IHostBuilder CreateHostBuilderMultiHttpClients(string[] args) =>
       Host.CreateDefaultBuilder(args)
           .ConfigureAppConfiguration((hostingContext, config) =>
            {
                config.AddJsonFile("appsettings.MultipleHttpClients.json", optional: false, reloadOnChange: true);
            })
           .ConfigureServices((context, services) =>
           {
               IConfiguration configuration = context.Configuration;

               services.AddDistributedMemoryCache();
               services.AddTransient<IClientAssertionService, ClientCredentialsAssertionService>();
               services.AddHostedService<WorkerMultipleHttpClients>();

               //Register APIs with Client credentials authentication
               services.AddClientCredentialsTokenManagement();
               var helseAIdApiOption = AddHelseIdProtectedApi(services, configuration);
               var duendeProtectedApi = AddDuendeProtectedApi(services, configuration);

               services.AddInMemoryDiscoveryService([
                   helseAIdApiOption.Authentication.Authority,
                   duendeProtectedApi.Authentication.Authority
                   ]);

               services.AddInMemoryDiscoveryService([
                   new DiscoveryDocumentStoreOptions
                   {
                       Authority = helseAIdApiOption.Authentication.Authority,
                       CacheDuration = TimeSpan.FromHours(24)
                   },
                    new DiscoveryDocumentStoreOptions
                   {
                       Authority = duendeProtectedApi.Authentication.Authority,
                       CacheDuration = TimeSpan.FromHours(48)
                   }
                  ]);
           });

    /// <summary>
    /// Authenticate with PrivateJwk
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configuration"></param>
    /// <returns></returns>
    public static HelseIdProtectedApiOption AddHelseIdProtectedApi(
           IServiceCollection services,
           IConfiguration configuration)
    {
        /***********************************************************************************************
         * Bind and validate API options
         * *********************************************************************************************/
        var helseIdProtectedApiSection = configuration.GetSection("Apis:HelseIdProtectedApi");
        services
            .AddOptions<HelseIdProtectedApiOption>()
             .Bind(helseIdProtectedApiSection)
             .ValidateDataAnnotations()
             .ValidateOnStart();

        /***********************************************************************************************
         * Configure Client credentials options used by the HttpClient to authenticate
         * *********************************************************************************************/
        var helseIdProtectedApi = helseIdProtectedApiSection.Get<HelseIdProtectedApiOption>() ?? default;

        services
          .AddOptions<ClientAssertionOptions>(helseIdProtectedApi!.ClientName)
          .Configure<IDiscoveryDocumentStore>((options, discoveryStore) =>
          {
              var discoveryDocument = discoveryStore.Get(helseIdProtectedApi!.Authentication.Authority);
              options.Issuer = discoveryDocument?.Issuer ?? string.Empty;
              options.PrivateJwk = helseIdProtectedApi.Authentication.PrivateJwk;
          });

        services
            .AddOptions<ClientCredentialsClient>(helseIdProtectedApi!.ClientName)
            .Configure<IDiscoveryDocumentStore>((options, discoveryStore) =>
            {
                var discoveryDocument = discoveryStore.Get(helseIdProtectedApi!.Authentication.Authority);
                options.TokenEndpoint = discoveryDocument?.TokenEndpoint is not null ? new Uri(discoveryDocument.TokenEndpoint) : null;
                options.ClientId = ClientId.Parse(helseIdProtectedApi.Authentication.ClientId);
                options.Scope = Scope.Parse(helseIdProtectedApi.Authentication.Scope);
            });

        /***********************************************************************************************
         * Register HttpClient and connect the token client to be used for authentiation
         * *********************************************************************************************/
        services.AddClientCredentialsHttpClient(helseIdProtectedApi!.ClientName, ClientCredentialsClientName.Parse(helseIdProtectedApi.ClientName), client =>
        {
            client.BaseAddress = new Uri(helseIdProtectedApi?.BaseAddress!);
        });
        return helseIdProtectedApi;
    }

    /// <summary>
    /// Authenticate with SharedSecret
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configuration"></param>
    /// <returns></returns>
    public static DuendeProtectedApiOption AddDuendeProtectedApi(
           IServiceCollection services,
           IConfiguration configuration)
    {
        /***********************************************************************************************
         * Bind and validate API options
         * *********************************************************************************************/
        var duendeProtectedApiSection = configuration.GetSection("Apis:DuendeProtetedApi");
        services
            .AddOptions<DuendeProtectedApiOption>()
             .Bind(duendeProtectedApiSection)
             .ValidateDataAnnotations()
             .ValidateOnStart();

        /***********************************************************************************************
         * Configure Client credentials options used by the HttpClient to authenticate
         * *********************************************************************************************/
        var duendeProtectedApi = duendeProtectedApiSection.Get<DuendeProtectedApiOption>() ?? default;
        services
            .AddOptions<ClientCredentialsClient>(duendeProtectedApi!.ClientName)
            .Configure<IDiscoveryDocumentStore>((options, discoveryStore) =>
            {
                var discoveryDocument = discoveryStore.Get(duendeProtectedApi!.Authentication.Authority);
                options.TokenEndpoint = discoveryDocument?.TokenEndpoint is not null ? new Uri(discoveryDocument.TokenEndpoint) : null;
                options.ClientId = ClientId.Parse(duendeProtectedApi.Authentication.ClientId);
                options.Scope = Scope.Parse(duendeProtectedApi.Authentication.Scope);
                options.ClientSecret = ClientSecret.Parse(duendeProtectedApi.Authentication.SharedSecret);
            })
            .Validate(
             clientCredential =>
                 !string.IsNullOrWhiteSpace(clientCredential.ClientId)
                 && !string.IsNullOrWhiteSpace(clientCredential?.TokenEndpoint?.AbsoluteUri),
                 failureMessage: "ClientId and TokenEndpoint must be provided and not empty."
             );

        /***********************************************************************************************
         * Register HttpClient and connect the token client to be used for authentiation
         * *********************************************************************************************/
        services.AddClientCredentialsHttpClient(duendeProtectedApi!.ClientName, ClientCredentialsClientName.Parse(duendeProtectedApi.ClientName), client =>
        {
            client.BaseAddress = new Uri(duendeProtectedApi?.BaseAddress!);
        });
        return duendeProtectedApi;
    }

}

