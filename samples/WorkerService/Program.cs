using Duende.IdentityModel;
using Duende.IdentityModel.Client;
using Fhi.Authentication.Tokens;
using WorkerService;

var builder = Host.CreateApplicationBuilder(args);

var api1Section = builder.Configuration.GetSection("ApiClientSample1");
builder.Services
    .AddOptions<ApiClientSample1>()
    .Bind(api1Section)
    .ValidateDataAnnotations()
    .ValidateOnStart();

var clientBuilder = builder.Services.AddClientCredentialsTokenManagement();

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSingleton<IOidcDiscoveryService, DefaultOidcDiscoveryService>();
builder.Services.AddHostedService<Worker>();

var api1Config = api1Section.Get<ApiClientSample1>();
using var scope = builder.Services.BuildServiceProvider().CreateScope();
var discoveryService = scope.ServiceProvider.GetRequiredService<IOidcDiscoveryService>();
var metadata = await discoveryService.GetDiscoveryDocument(api1Config!.ClientAuthentication.Authority);

//builder.Services
//    .AddOptions<ClientCredentialsClient>("api")
//    .Configure<IOidcDiscoveryService>((options) =>
//    {
//        options.TokenEndpoint = metadata.TokenEndpoint;
//        options.ClientId = api1Config.ClientAuthentication.ClientId;
//        options.Scope = api1Config.ClientAuthentication.Scope;
//        options.DPoPJsonWebKey = api1Config.ClientAuthentication.Secret;
//        options.Parameters = new Parameters()
//        {
//            { OidcConstants.TokenRequest.ClientAssertionType, OidcConstants.ClientAssertionTypes.JwtBearer },
//            { OidcConstants.TokenRequest.ClientAssertion, Fhi.Authentication.Tokens.ClientAssertionTokenHandler.CreateJwtToken(
//                api1Config.ClientAuthentication.Authority,
//                api1Config.ClientAuthentication.ClientId,
//                api1Config.ClientAuthentication.Secret) }
//        };
//    });
clientBuilder
    .AddClient("api", options =>
    {
        options.TokenEndpoint = metadata.TokenEndpoint;
        options.ClientId = api1Config!.ClientAuthentication.ClientId;
        options.Scope = api1Config.ClientAuthentication.Scope;
        options.DPoPJsonWebKey = api1Config.ClientAuthentication.Secret;
        options.Parameters = new Parameters()
        {
            { OidcConstants.TokenRequest.ClientAssertionType, OidcConstants.ClientAssertionTypes.JwtBearer },
            { OidcConstants.TokenRequest.ClientAssertion, ClientAssertionTokenHandler.CreateJwtToken(
                api1Config.ClientAuthentication.Authority,
                api1Config.ClientAuthentication.ClientId,
                api1Config.ClientAuthentication.Secret) }
        };
    });
builder.Services.AddClientCredentialsHttpClient("api", "api", (client) =>
{
    client.BaseAddress = new Uri(api1Config!.BaseAddress!);
});

var host = builder.Build();
host.Run();


