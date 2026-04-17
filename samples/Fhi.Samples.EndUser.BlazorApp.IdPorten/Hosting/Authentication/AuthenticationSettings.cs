namespace BlazorApp.IdPorten.Hosting.Authentication;

public class AuthenticationSettings
{
    public required string Authority { get; set; }
    public required string ClientId { get; set; }

    /// <summary>
    /// PEM-encoded RSA private key used to sign JWT client assertions.
    /// Store this value in User Secrets, not in appsettings files.
    /// </summary>
    public required string ClientSecret { get; set; }

    /// <summary>
    /// The audience claim for the JWT client assertion — typically the token endpoint URL.
    /// For ID-Porten test: "https://test.idporten.no"
    /// For Ansattporten test: "https://test.ansattporten.no"
    /// </summary>
    public required string ClientAssertionAudience { get; set; }

    /// <summary>
    /// The key ID (kid) matching the public key registered with ID-Porten for this client.
    /// </summary>
    public required string ClientAssertionKeyId { get; set; }

    public required string Scopes { get; set; }
}
