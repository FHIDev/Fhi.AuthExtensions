using Duende.AccessTokenManagement;
using Microsoft.Extensions.Options;

namespace WorkerService.Workers
{
    /// <summary>
    /// This worker is used to create bearer token using client credentials flow. 
    /// It contains sample of using HttpClient extension from duende and using HttpRequest.
    /// </summary>
    internal class ClientCredentialBearerTokenWorkerSample : BackgroundService
    {
        private readonly ILogger<ClientCredentialBearerTokenWorkerSample> _logger;
        private readonly IHttpClientFactory _factory;
        private readonly IClientCredentialsTokenManagementService _clientCredentialsTokenManagement;
        private readonly ClientConfiguration _clientConfiguration;

        public ClientCredentialBearerTokenWorkerSample(
            ILogger<ClientCredentialBearerTokenWorkerSample> logger,
            IHttpClientFactory factory,
            IOptions<ClientConfiguration> clientConfigurations,
            IClientCredentialsTokenManagementService clientCredentialsTokenManagement)
        {
            _logger = logger;
            _factory = factory;
            _clientCredentialsTokenManagement = clientCredentialsTokenManagement;
            _clientConfiguration = clientConfigurations.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            var healthApiClient = _factory.CreateClient(_clientConfiguration.ClientName);

            /*****************************************************************************
             * Request to the integration endpoint API using HttpClient extension from Duende
             *****************************************************************************/
            var response = await healthApiClient.GetAsync("api/v1/integration/health-records");
            _logger.LogInformation("Using HttpClient Response: " + await response.Content.ReadAsStringAsync());

            /*****************************************************************************
            * Request to the end-user endpoint API using HttpClient extension from Duende.
            * The client should not have access to this endpoint, so it should return 403 Forbidden.
            *****************************************************************************/
            var meResponse = await healthApiClient.GetAsync("api/v1/me/health-records");
            if (meResponse.IsSuccessStatusCode)
                throw new Exception("User should not have access");
            _logger.LogInformation("Access denied response: " + meResponse.StatusCode);

            await RunService(cancellationToken);
        }

        private async Task RunService(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                }
                await Task.Delay(1000, cancellationToken);
            }
        }
    }
}
