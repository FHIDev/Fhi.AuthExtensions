using Fhi.Samples.WorkerServiceMultipleClients.Configurations;
using Microsoft.Extensions.Options;

namespace Fhi.Samples.WorkerServiceMultipleClients
{
    internal class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IHttpClientFactory _factory;
        private readonly IOptions<ApiClientSample1> _optionsApi1;
        private readonly IOptions<ApiClientSample2> _optionsApi2;

        public Worker(
           ILogger<Worker> logger,
           IHttpClientFactory factory,
           IOptions<ApiClientSample1> optionsApi1,
           IOptions<ApiClientSample2> optionsApi2)
        {
            _logger = logger;
            _factory = factory;
            _optionsApi1 = optionsApi1;
            _optionsApi2 = optionsApi2;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var api1Client = _factory.CreateClient(_optionsApi1.Value.ClientName);
            //var responseApi1 = await api1Client.GetAsync("api/v2/integration/health-records");
            var responseApi1 = await api1Client.GetAsync("weatherforecast");

            var api2Client = _factory.CreateClient(_optionsApi2.Value.ClientName);
            var responseApi2 = await api2Client.GetAsync("api/v1/integration/health-records");

            while (!stoppingToken.IsCancellationRequested)
            {
                if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                }
                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}
