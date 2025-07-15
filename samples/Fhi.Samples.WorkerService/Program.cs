using Duende.AccessTokenManagement;
using WorkerService;
using WorkerService.Workers;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<ClientCredentialDPoPTokenWorker>();
builder.Services.AddHostedService<ClientCredentialBearerTokenDuendeHttpDelegationHandlerSample>();

/***************************************************************************************** 
 * Step 0: Register ClientConfiguration to read client settings from appsettings.json. 
 *****************************************************************************************/
var clientSection = builder.Configuration.GetSection("ClientConfiguration");
builder.Services.AddOptions<ClientConfiguration>()
    .Bind(clientSection)
    .ValidateDataAnnotations()
    .ValidateOnStart();

var clientConfiguration = clientSection.Get<ClientConfiguration>() ?? new ClientConfiguration();

/***************************************************************************************** 
 * Step 1: Regiser HttpClients with Duende Access Token Management package to handle token requests.
 *****************************************************************************************/
//Sample 1: Client with Bearer
builder.Services
    .AddClientCredentialsTokenManagement()
    .AddClient(clientConfiguration.ClientName, options =>
    {
        options.TokenEndpoint = clientConfiguration.TokenEndpoint;
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
        //Can use client assertion key or generate a new
        options.DPoPJsonWebKey = clientConfiguration.Secret;
    });

builder.Services.AddClientCredentialsHttpClient(clientConfiguration.ClientName + ".dpop", clientConfiguration.ClientName + ".dpop", client =>
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
