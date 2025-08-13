namespace Fhi.Samples.WebApi.Api.HealthRecord.Me.v1.Dtos
{
    public record HealthRecordPersonDto(string Pid, string Name, string Description, DateTime CreatedAt);
}
