// Using Refit to define the API contract
using Refit;

internal record HealthRecordDto(string Name, string Description, DateTime CreatedAt);
internal interface IHealthRecordApi
{
    [Get("/api/v1/integration/health-records")]
    Task<IEnumerable<HealthRecordDto>> GetHealthRecordsAsync();
}