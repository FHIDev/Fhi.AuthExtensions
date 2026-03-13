namespace Fhi.Samples.EndUser.Angular.BFFApi.Services.Models
{
    public class TokenSet
    {
        public string AccessToken { get; set; } = "";
        public string RefreshToken { get; set; } = "";
        public string IdToken { get; set; } = "";
        public DateTimeOffset ExpiresAt { get; set; }
    }
}