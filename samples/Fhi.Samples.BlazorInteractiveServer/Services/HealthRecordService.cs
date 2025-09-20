
namespace Client.BlazorInteractiveServer.Services
{
    public class HealthRecord
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
    }
    public class HealthRecordService : BaseService
    {
        private readonly IHttpClientFactory _factory;

        public HealthRecordService(IHttpClientFactory factory, NavigationService navigationService) : base(navigationService)
        {
            _factory = factory;
        }

        public async Task<ServiceResult<IEnumerable<HealthRecord>>> GetHealthrecords()
        {
            return await ExecuteWithErrorHandling<IEnumerable<HealthRecord>>(async () =>
            {
                var client = _factory.CreateClient("WebApi");
                return await client.GetAsync("/api/v1/me/health-records/helseid-bearer");
            });
        }

        public async Task<ServiceResult<IEnumerable<HealthRecord>>> GetHealthrecordsFromIntegrationEndpoint()
        {
            return await ExecuteWithErrorHandling<IEnumerable<HealthRecord>>(async () =>
            {
                var client = _factory.CreateClient("WebApi");
                return await client.GetAsync("/api/v1/integration/health-records/helseid-bearer");
            });
        }
    }
}
