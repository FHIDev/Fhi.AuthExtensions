namespace Fhi.Samples.WebApi.IdPorten.Hosting;

internal class AuthenticationSettings
{
    public string Authority { get; set; } = string.Empty;
    public string? Audience { get; set; }
}
