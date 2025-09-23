namespace Fhi.Samples.WebApi.Api.HealthRecord.Me.v1.Dtos
{
    public record UserDto(string? Name, IList<ClaimDto> Claims, string Scheme);
}
