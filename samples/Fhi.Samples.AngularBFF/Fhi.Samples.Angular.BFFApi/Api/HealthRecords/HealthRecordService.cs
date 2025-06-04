using System.Text.Json;
using System.Text.Json.Serialization;

namespace AngularBFF.Net8.Api.HealthRecords
{
    public record HealthRecordModel(string Pid, string Name, string Description, DateTime CreatedAt);

    public interface IHealthRecordService
    {
        public Task<IEnumerable<HealthRecordModel>?> GetRecords();
    }

    /// <summary>
    /// Calling external API to get weather forcast
    /// </summary>
    public class HealthRecordService : IHealthRecordService
    {
        public HealthRecordService(IHttpClientFactory factory)
        {
            Factory = factory;
        }
        public IHttpClientFactory Factory { get; }

        public async Task<IEnumerable<HealthRecordModel>?> GetRecords()
        {
            var client = Factory.CreateClient("WebApi");
            var response = await client.GetAsync("api/v1/me/health-records");
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                throw new UnauthorizedAccessException();
            }
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<IEnumerable<HealthRecordModel>>(
                content,
                new JsonSerializerOptions()
                {
                    PropertyNameCaseInsensitive = true,
                    IncludeFields = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                });
        }
    }
}
