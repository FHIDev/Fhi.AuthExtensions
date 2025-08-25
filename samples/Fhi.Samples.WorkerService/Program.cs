using Duende.AccessTokenManagement;
using Duende.IdentityModel;
using Duende.IdentityModel.Client;
using Fhi.Authentication.Tokens;
using Fhi.Samples.WorkerService.Workers;
using Microsoft.Extensions.Options;
using Refit;
using WorkerService;
using WorkerService.Workers;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddHostedService<ClientCredentialDPoPTokenWorkerSample>();
builder.Services.AddHostedService<ClientCredentialBearerTokenWorkerSample>();
builder.Services.AddHostedService<ClientCredentialsRefitWorkerSample>();

/***************************************************************************************** 
 * Step 0: Register ClientConfiguration to read client settings from appsettings.json. 
 *****************************************************************************************/
var clientSection = builder.Configuration.GetSection("ClientConfiguration");
builder.Services.AddOptions<ClientConfiguration>()
    .Bind(clientSection)
    .ValidateDataAnnotations()
    .ValidateOnStart();

var clientConfiguration = clientSection.Get<ClientConfiguration>() ?? new ClientConfiguration();

builder.Services.AddSingleton(new DiscoveryCache(clientConfiguration.Authority));
builder.Services.AddSingleton<IConfigureOptions<ClientCredentialsClient>, ClientCredentialsClientConfigureOptions>();


/***************************************************************************************** 
 * Step 1: Regiser HttpClients with Duende Access Token Management package to handle token requests.
 *****************************************************************************************/
//Sample 1: Client with Bearer
builder.Services
    .AddClientCredentialsTokenManagement()
    .AddClient(clientConfiguration.ClientName, options =>
    {
        //options.TokenEndpoint = clientConfiguration.TokenEndpoint;
        options.ClientId = clientConfiguration.ClientId;
        options.Scope = clientConfiguration.Scope;
    });

builder.Services.AddClientCredentialsHttpClient(clientConfiguration.ClientName, clientConfiguration.ClientName, client =>
{
    client.BaseAddress = new Uri("https://localhost:7150");
});

//Sample 2: Client with DPoP
builder.Services
    .AddClientCredentialsTokenManagement()
    .AddClient(clientConfiguration.ClientName + ".dpop", options =>
    {
        options.TokenEndpoint = clientConfiguration.TokenEndpoint;
        options.ClientId = clientConfiguration.ClientId;
        options.Scope = clientConfiguration.Scope;
        //Can use existing secret as key or generate a new key for DPoP proof
        options.DPoPJsonWebKey = clientConfiguration.Secret;
        options.Parameters = new Parameters()
        {
            { OidcConstants.TokenRequest.ClientAssertionType, OidcConstants.ClientAssertionTypes.JwtBearer },
            { OidcConstants.TokenRequest.ClientAssertion, ClientAssertionTokenHandler.CreateJwtToken(clientConfiguration.Authority, clientConfiguration.ClientId, clientConfiguration.Secret) }
        };
    });

builder.Services.AddClientCredentialsHttpClient(clientConfiguration.ClientName + ".dpop", clientConfiguration.ClientName + ".dpop", client =>
{
    client.BaseAddress = new Uri("https://localhost:7150");
});

//Sample 3: Client with Refit
builder.Services
    .AddClientCredentialsTokenManagement()
    .AddClient(clientConfiguration.ClientName + ".refit", options =>
    {
        options.TokenEndpoint = clientConfiguration.TokenEndpoint;
        options.ClientId = clientConfiguration.ClientId;
        options.Scope = clientConfiguration.Scope;

    });

builder.Services.AddClientCredentialsHttpClient(clientConfiguration.ClientName + ".refit", clientConfiguration.ClientName + ".refit", client =>
{
    client.BaseAddress = new Uri("https://localhost:7150");
})
.AddTypedClient(RestService.For<IHealthRecordApi>);

//Sample 4: Client with ClientAssertion
builder.Services
    .AddClientCredentialsTokenManagement()
    .AddClient(clientConfiguration.ClientName + ".clientassertion", options =>
    {
        options.TokenEndpoint = clientConfiguration.TokenEndpoint;
        options.ClientId = clientConfiguration.ClientId;
        options.Scope = clientConfiguration.Scope;
        options.Parameters = new Parameters()
        {
            { OidcConstants.TokenRequest.ClientAssertionType, OidcConstants.ClientAssertionTypes.JwtBearer },
            { OidcConstants.TokenRequest.ClientAssertion, ClientAssertionTokenHandler.CreateJwtToken(clientConfiguration.Authority, clientConfiguration.ClientId, clientConfiguration.Secret) }
        };
    });
builder.Services.AddClientCredentialsHttpClient(clientConfiguration.ClientName + ".clientassertion", clientConfiguration.ClientName + ".clientassertion", client =>
{
    client.BaseAddress = new Uri("https://localhost:7150");
});



/***************************************************************************************** 
 * Step 2: In order to use asymetric JWK key secret, ClientAssertion, for client credentials flow, we 
 * need to register a service that will handle the creation of the assertion.
 *****************************************************************************************/
builder.Services.AddTransient<IClientAssertionService, ClientCredentialAssertionService>();
builder.Services.AddDistributedMemoryCache();

var host = builder.Build();
host.Run();
