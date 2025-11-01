using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace M2M.Host.Maskinporten
{
    internal class HealthRecordService
    {
        private readonly ILogger<HealthRecordService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly MaskinPortenProtectedApiOption _maskinPortenProtectedApiOption;

        public HealthRecordService(
            ILogger<HealthRecordService> logger,
            IHttpClientFactory httpClientFactory,
            IOptions<MaskinPortenProtectedApiOption> maskinPortenProtectedApiOption)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _maskinPortenProtectedApiOption = maskinPortenProtectedApiOption.Value;
        }

        public async Task<string> GetHealthRecords()
        {
            var client = _httpClientFactory.CreateClient(_maskinPortenProtectedApiOption.ClientName);
            client.BaseAddress = new Uri(_maskinPortenProtectedApiOption.BaseAddress!);
            var response = await client.GetAsync("api/v1/integration/health-records/maskinporten");

            _logger.LogInformation("Request: {req}", response.RequestMessage);
            _logger.LogInformation("Response: {response}", response);
            return await response.Content.ReadAsStringAsync();
        }
    }
}
