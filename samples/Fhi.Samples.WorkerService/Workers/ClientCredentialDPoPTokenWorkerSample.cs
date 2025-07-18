using Duende.AccessTokenManagement;
using Microsoft.Extensions.Options;

namespace WorkerService.Workers
{
    public class ClientCredentialDPoPTokenWorkerSample : BackgroundService
    {
        private readonly ILogger<ClientCredentialDPoPTokenWorkerSample> _logger;
        private readonly IHttpClientFactory _factory;
        private readonly IDPoPProofService _dPoPProofService;
        private readonly ClientConfiguration _clientConfiguration;

        public ClientCredentialDPoPTokenWorkerSample(
            ILogger<ClientCredentialDPoPTokenWorkerSample> logger,
            IHttpClientFactory factory,
            IOptions<ClientConfiguration> clientConfigurations,
            IDPoPProofService dPoPProofService)
        {
            _logger = logger;
            _factory = factory;
            _dPoPProofService = dPoPProofService;
            _clientConfiguration = clientConfigurations.Value;
        }

        /// <summary>
        /// Manually getting Dpop token and set authorization header on the API request. 
        /// </summary>
        /// <param name="stoppingToken"></param>
        /// <returns></returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // NB! This will give 401. API does not yet support DPoP.
            var healthRecordApiDPoPClient = _factory.CreateClient(_clientConfiguration.ClientName + ".dpop");
            var dpopResponse = await healthRecordApiDPoPClient.GetAsync("api/v1/integration/health-records");
            _logger.LogInformation("Dpop weather response: " + await dpopResponse.Content.ReadAsStringAsync());

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
