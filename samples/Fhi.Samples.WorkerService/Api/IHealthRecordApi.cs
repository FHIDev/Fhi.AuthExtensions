using Refit;

namespace WorkerService.Api
{
    public record HealthRecordDto(string Name, string Description, DateTime CreatedAt);

    public interface IHealthRecordApi
    {
        [Get("/api/v1/integration/health-records")]
        Task<IEnumerable<HealthRecordDto>> GetHealthRecordsAsync();
    }
}
