namespace AngularBFF.Net8.Api.Api.UserInformation.V1.Dtos
{
    public record UserSessionDto(string? AccessToken, string? IdToken, string? refreshToken);
}
