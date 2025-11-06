using Client.ClientCredentialsWorkers.MultipleHttpClients.Options;
using Microsoft.Extensions.Options;

namespace Fhi.Samples.WorkerServiceMultipleClients.MultipleClientVariant2
{
    internal class WorkerMultipleHttpClients : BackgroundService
    {
        private readonly HelseIdProtectedApiOption _helseIdProtectedApiOption;
        private readonly ILogger<WorkerMultipleHttpClients> _logger;
        private readonly IHttpClientFactory _factory;
        private readonly DuendeProtectedApiOption _duendeProtectedApiOption;

        public WorkerMultipleHttpClients(
           ILogger<WorkerMultipleHttpClients> logger,
           IHttpClientFactory factory,
           IOptions<HelseIdProtectedApiOption> helseIdProtectedApiOption,
           IOptions<DuendeProtectedApiOption> duendeProtectedApiOption)
        {
            _helseIdProtectedApiOption = helseIdProtectedApiOption.Value;
            _logger = logger;
            _factory = factory;
            _duendeProtectedApiOption = duendeProtectedApiOption.Value;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var api1Client = _factory.CreateClient(_helseIdProtectedApiOption.ClientName);
                var helseIdBearerResponse = await api1Client.GetAsync("api/v1/integration/health-records/helseid-bearer");
                _logger.LogInformation("Request: {req}", helseIdBearerResponse.RequestMessage);
                _logger.LogInformation("Response: {response}", helseIdBearerResponse);

                ////TODO: when DPoP is supported
                ////var api2Client = _factory.CreateClient(xx.ClientName);
                ////var helseIdDpopResponse = await api2Client.GetAsync("api/v1/integration/health-records/helseid-dpop");
                ////_logger.LogInformation("Request: {req}", helseIdDpopResponse.RequestMessage);
                ////_logger.LogInformation("Response: {response}", helseIdDpopResponse);


                var api2Client = _factory.CreateClient(_duendeProtectedApiOption.ClientName);
                var responseApi2 = await api2Client.GetAsync("api/v1/integration/health-records/duende");
                _logger.LogInformation("Request: {req}", responseApi2.RequestMessage);
                _logger.LogInformation("Response: {response}", responseApi2);

                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

                await Task.Delay(5000, stoppingToken);
            }
        }
    }
}
