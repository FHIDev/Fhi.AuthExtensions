using Refit;

namespace Fhi.Samples.WorkerService.Workers
{
    // Using Refit to define the API contract
    internal record HealthRecordDto(string Name, string Description, DateTime CreatedAt);
    internal interface IHealthRecordApi
    {
        [Get("/api/v1/integration/health-records")]
        Task<IEnumerable<HealthRecordDto>> GetHealthRecordsAsync();
    }

    internal class ClientCredentialsRefitWorkerSample : BackgroundService
    {
        private readonly IHealthRecordApi _healthRecordApi;
        private readonly ILogger<ClientCredentialsRefitWorkerSample> _logger;

        public ClientCredentialsRefitWorkerSample(
            IHealthRecordApi healthRecordApi,
            ILogger<ClientCredentialsRefitWorkerSample> logger)
        {
            _healthRecordApi = healthRecordApi;
            _logger = logger;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var response = await _healthRecordApi.GetHealthRecordsAsync();

            _logger.LogInformation("Refit response: {Response}", response);
        }
    }
}
