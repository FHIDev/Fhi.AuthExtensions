namespace BlazorInteractiveServer.Hosting.Authentication
{
    public class AuthenticationSettings
    {
        public required string Authority { get; set; }
        public required string ClientId { get; set; }
        public required string ClientSecret { get; set; }
        public required string Scopes { get; set; }

        public string ClientAssertionAudinece { get; set; } = string.Empty;

        public string ClientAssertionKid { get; set; } = string.Empty;
    }
}
