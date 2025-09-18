using Microsoft.Extensions.Options;

namespace Fhi.Samples.WorkerServiceMultipleClients.Refit
{
    internal class WorkerRefit : BackgroundService
    {
        private readonly ILogger<WorkerRefit> _logger;
        private readonly IOptions<ApiOption> _options;
        private readonly IHttpClientFactory _factory;

        public WorkerRefit(
           ILogger<WorkerRefit> logger,
           IOptions<ApiOption> options,
           IHttpClientFactory factory)
        {
            _logger = logger;
            _options = options;
            _factory = factory;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var api1Client = _factory.CreateClient(_options.Value.ClientName);
                var helseIdBearerResponse = await api1Client.GetAsync("api/v1/integration/health-records/helseid-bearer");
                _logger.LogInformation("Request: {req}", helseIdBearerResponse.RequestMessage);
                _logger.LogInformation("Response: {response}", helseIdBearerResponse);
            }
        }
    }
}
