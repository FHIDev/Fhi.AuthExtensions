using Fhi.Authentication;
using Fhi.Samples.RefitClientCredentials.Services; // For IHealthRecordsApi
using Fhi.Samples.RefitClientCredentials.Workers; // For HealthRecordsWorker and WorkerServiceOptions

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

// Bind WorkerServiceOptions from configuration
builder.Services.Configure<WorkerServiceOptions>(builder.Configuration.GetSection("WorkerService"));

// Add and configure Refit client with client credentials for Health Records API
// This uses the extension method from Fhi.Authentication.Extensions
// The client will automatically handle OAuth 2.0 token acquisition and renewal
builder.Services.AddRefitClientWithClientCredentials<IHealthRecordsApi>(builder.Configuration);

// Add the Health Records worker service
builder.Services.AddHostedService<HealthRecordsWorker>();

var host = builder.Build();
host.Run();
