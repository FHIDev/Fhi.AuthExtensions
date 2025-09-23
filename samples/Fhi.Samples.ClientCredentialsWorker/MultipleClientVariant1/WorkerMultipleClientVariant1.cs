using Fhi.Samples.WorkerServiceMultipleClients.MultipleClientVariant1.Configurations;
using Microsoft.Extensions.Options;

namespace Fhi.Samples.WorkerServiceMultipleClients.MultipleClientVariant1
{
    internal class WorkerMultipleClientVariant1 : BackgroundService
    {
        private readonly ILogger<WorkerMultipleClientVariant1> _logger;
        private readonly IHttpClientFactory _factory;
        private readonly IOptions<ApiClientSample1> _optionsApi1;
        private readonly IOptions<ApiClientSample2> _optionsApi2;

        public WorkerMultipleClientVariant1(
           ILogger<WorkerMultipleClientVariant1> logger,
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
            while (!stoppingToken.IsCancellationRequested)
            {
                var api1Client = _factory.CreateClient(_optionsApi1.Value.ClientName);
                var helseIdBearerResponse = await api1Client.GetAsync("api/v1/integration/health-records/helseid-bearer");
                _logger.LogInformation("Request: {req}", helseIdBearerResponse.RequestMessage);
                _logger.LogInformation("Response: {response}", helseIdBearerResponse);

                //TODO: when DPoP is supported
                //var helseIdDpopResponse = await api1Client.GetAsync("api/v1/integration/health-records/helseid-dpop");
                //_logger.LogInformation("Request: {req}", helseIdDpopResponse.RequestMessage);
                //_logger.LogInformation("Response: {response}", helseIdDpopResponse);


                var api2Client = _factory.CreateClient(_optionsApi2.Value.ClientName);
                var responseApi2 = await api2Client.GetAsync("api/v1/integration/health-records/duende");
                _logger.LogInformation("Request: {req}", responseApi2.RequestMessage);
                _logger.LogInformation("Response: {response}", responseApi2);

                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                await Task.Delay(5000, stoppingToken);
            }
        }
    }
}
