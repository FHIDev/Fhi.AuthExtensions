using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace M2M.Host.HelseID
{
    internal class HealthRecordService
    {
        private readonly ILogger<HealthRecordService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly HelseIdProtectedApiOption _protectedApiOption;

        public HealthRecordService(
            ILogger<HealthRecordService> logger,
            IHttpClientFactory httpClientFactory,
            IOptions<HelseIdProtectedApiOption> protectedApiOption)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _protectedApiOption = protectedApiOption.Value;
        }

        public async Task<string> GetHealthRecords()
        {
            var client = _httpClientFactory.CreateClient(_protectedApiOption.ClientName);
            client.BaseAddress = new Uri(_protectedApiOption.BaseAddress!);
            var response = await client.GetAsync("api/v1/integration/health-records/helseid-bearer");
            _logger.LogInformation("Request: {req}", response.RequestMessage);
            _logger.LogInformation("Response: {response}", response);

            return await response.Content.ReadAsStringAsync();
        }
    }
}
