using Duende.AccessTokenManagement;
using Fhi.Authentication;
using Fhi.Authentication.ClientCredentials;
using Fhi.Authentication.OpenIdConnect;
using Fhi.Samples.WorkerServiceMultipleClients.MultipleClientVariant2;

public partial class Program
{
    public static IHostBuilder CreateHostBuilderMultiClientVariant2(string[] args) =>
       Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((hostingContext, config) =>
            {
                config.AddJsonFile("appsettings.MultipleClientVariant2.json", optional: false, reloadOnChange: true);
            })
           .ConfigureServices((context, services) =>
           {
               IConfiguration configuration = context.Configuration;

               services.AddDistributedMemoryCache();
               services.AddTransient<IClientAssertionService, ClientCredentialsAssertionService>();
               services.AddHostedService<WorkerMultipleClientVariant2>();

               //Register APIs with Client credentials authentication
               var helseAIdApiOption = AddHelseIdProtectedApi(services, configuration);
               var duendeProtectedApi = AddDuendeProtectedApi(services, configuration);

               services.AddInMemoryDiscoveryService([helseAIdApiOption.Authentication.Authority, duendeProtectedApi.Authentication.Authority]);
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
            .AddOptions<ClientCredentialsClient>(helseIdProtectedApi!.ClientName)
            .Configure<IDiscoveryDocumentStore>((options, discoveryStore) =>
            {
                var discoveryDocument = discoveryStore.Get(helseIdProtectedApi!.Authentication.Authority);
                options.TokenEndpoint = discoveryDocument.TokenEndpoint;
                options.ClientId = helseIdProtectedApi.Authentication.ClientId;
                options.Scope = helseIdProtectedApi.Authentication.Scope;
                //To enable DPoP
                //options.DPoPJsonWebKey = helseIdProtectedApi.Authentication.PrivateJwk;
                options.Parameters = new ClientCredentialParametersBuilder()
                      .AddIssuer(discoveryDocument.Issuer ?? string.Empty)
                      .AddPrivateJwk(helseIdProtectedApi.Authentication.PrivateJwk)
                      .Build();
            })
            .Validate(
             clientCredential =>
                 !string.IsNullOrWhiteSpace(clientCredential.ClientId)
                 && !string.IsNullOrWhiteSpace(clientCredential.TokenEndpoint),
                 failureMessage: "ClientId and TokenEndpoint must be provided and not empty."
             );


        /***********************************************************************************************
         * Register HttpClient and connect the token client to be used for authentiation
         * *********************************************************************************************/
        services.AddClientCredentialsHttpClient(helseIdProtectedApi!.ClientName, helseIdProtectedApi.ClientName, client =>
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
                options.TokenEndpoint = discoveryDocument.TokenEndpoint;
                options.ClientId = duendeProtectedApi.Authentication.ClientId;
                options.Scope = duendeProtectedApi.Authentication.Scope;
                options.ClientSecret = duendeProtectedApi.Authentication.SharedSecret;
                options.Parameters = new ClientCredentialParametersBuilder()
                      .AddIssuer(discoveryDocument.Issuer)
                      .Build();
            })
            .Validate(
             clientCredential =>
                 !string.IsNullOrWhiteSpace(clientCredential.ClientId)
                 && !string.IsNullOrWhiteSpace(clientCredential.TokenEndpoint),
                 failureMessage: "ClientId and TokenEndpoint must be provided and not empty."
             );

        /***********************************************************************************************
         * Register HttpClient and connect the token client to be used for authentiation
         * *********************************************************************************************/
        services.AddClientCredentialsHttpClient(duendeProtectedApi!.ClientName, duendeProtectedApi.ClientName, client =>
        {
            client.BaseAddress = new Uri(duendeProtectedApi?.BaseAddress!);
        });
        return duendeProtectedApi;
    }
}

