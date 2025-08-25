using Duende.AccessTokenManagement;
using Fhi.Samples.WorkerServiceMultipleClients;
using Microsoft.Extensions.Options;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();

var clientConfigurations = builder.Configuration.GetSection("HttpClientConfiguration");
var optionsBuilder = builder.Services.AddOptions<HttpClientConfiguration>()
    .Bind(clientConfigurations);

optionsBuilder
    //.ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services
    .AddClientCredentialsTokenManagement()
    .AddClient("webapi.duende.jwt", options => { })
    .AddClient("webapi.duende.sharedsecret", options => { });

builder.Services.AddSingleton<IConfigureNamedOptions<ClientCredentialsClient>, ClientCredentialsClientConfigureOptions>();
builder.Services.AddSingleton<IDiscoveryCacheFactory, DiscoveryCacheFactory>();
builder.Services.AddDistributedMemoryCache();

var host = builder.Build();
host.Run();
