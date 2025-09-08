using Duende.AccessTokenManagement;
using Duende.IdentityModel;
using Duende.IdentityModel.Client;
using Fhi.Samples.WorkerServiceMultipleClients;
using Fhi.Samples.WorkerServiceMultipleClients.Configurations;

var builder = Host.CreateApplicationBuilder(args);

/**********************************************************************
 *  1. Register the core services needed for OAuth client credentials flow and token management
 *  -  AddClientCredentialsTokenManagement(): Enables automatic token acquisition and refresh
 *  -  AddDistributedMemoryCache(): Provides token caching capabilities
 *  -  IOidcDiscoveryService: Discovers OIDC endpoints from authority metadata
 *  -  Worker: The background service that will consume the APIs
 **********************************************************************/

builder.Services.AddClientCredentialsTokenManagement();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSingleton<IOidcDiscoveryService, DefaultOidcDiscoveryService>();
builder.Services.AddHostedService<Worker>();

/*****************************************************************************
 * 2. API 1 Configuration - HelseID with Client Assertion
 * ****************************************************************************/

// Validate and bind configuration to use it in the Worker
var api1Section = builder.Configuration.GetSection("ApiClientSample1");
builder.Services
    .AddOptions<ApiClientSample1>()
    .Bind(api1Section)
    .ValidateDataAnnotations()
    .ValidateOnStart();

//Read API 1 configuration to use it to setup client authentication
var api1Config = api1Section.Get<ApiClientSample1>();
builder.Services
    .AddOptions<ClientCredentialsClient>(api1Config!.ClientName)
    .Configure<IOidcDiscoveryService>((options, discoveryService) =>
    {
        var metadata = discoveryService.GetDiscoveryDocument(api1Config!.ClientAuthentication.Authority).GetAwaiter().GetResult();
        options.TokenEndpoint = metadata.TokenEndpoint;
        options.ClientId = api1Config.ClientAuthentication.ClientId;
        options.Scope = api1Config.ClientAuthentication.Scope;
        options.DPoPJsonWebKey = api1Config.ClientAuthentication.Secret;
        options.Parameters = new Parameters()
        {
            { OidcConstants.TokenRequest.ClientAssertionType, OidcConstants.ClientAssertionTypes.JwtBearer },
            { OidcConstants.TokenRequest.ClientAssertion, Fhi.Authentication.Tokens.ClientAssertionTokenHandler.CreateJwtToken(
                api1Config.ClientAuthentication.Authority,
                api1Config.ClientAuthentication.ClientId,
                api1Config.ClientAuthentication.Secret) }
        };
    });
// Register the HttpClient for API 1, using the named client credentials configuration
builder.Services.AddClientCredentialsHttpClient(api1Config.ClientName, api1Config.ClientName, (sp, client) =>
{
    client.BaseAddress = new Uri(api1Config.BaseAddress!);
});

/*****************************************************************************
 * 3. API 2 Configuration - Duende with Shared Secret
 * ****************************************************************************/

// Validate and bind configuration to use it in the Worker
var api2Section = builder.Configuration.GetSection("ApiClientSample2");
builder.Services.AddOptions<ApiClientSample2>()
    .Bind(api2Section)
    .ValidateDataAnnotations()
    .ValidateOnStart();

var api2Config = api2Section.Get<ApiClientSample2>();
builder.Services
    .AddOptions<ClientCredentialsClient>(api2Config!.ClientName)
    .Configure<IOidcDiscoveryService>((options, discoveryService) =>
    {
        var metadata = discoveryService.GetDiscoveryDocument(api2Config!.ClientAuthentication!.Authority).GetAwaiter().GetResult();
        options.TokenEndpoint = metadata?.TokenEndpoint;
        options.ClientId = api2Config.ClientAuthentication.ClientId;
        options.Scope = api2Config.ClientAuthentication.Scope;
        options.ClientSecret = api2Config.ClientAuthentication.Secret;
    });

builder.Services.AddClientCredentialsHttpClient(api2Config.ClientName, api2Config.ClientName, (sp, client) =>
{
    client.BaseAddress = new Uri(api2Config.BaseAddress!);
});

/*****************************************************************************
 * 4. Start the Host
 * ****************************************************************************/

var host = builder.Build();
host.Run();
