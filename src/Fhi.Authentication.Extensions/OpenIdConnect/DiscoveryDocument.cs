namespace Fhi.Authentication.OpenIdConnect
{
    /// <summary>
    /// Represents a read-only set of OpenID Connect discovery document endpoints and metadata.
    /// </summary>
    /// <remarks>This interface provides access to standard OpenID Connect endpoints such as authorization,
    /// token, user info, and JSON Web Key Set (JWKS) URIs.</remarks>
    public interface IDiscoveryDocument
    {
        /// <summary>
        /// Gets the authority component of the URI, typically consisting of the host and optional port information.
        /// </summary>
        string? Authority { get; }
        /// <summary>
        /// Gets the identifier of the entity that issued the token or credential.
        /// </summary>
        string? Issuer { get; }
        /// <summary>
        /// Gets the URI of the authorization endpoint used for initiating the OAuth or OpenID Connect authorization
        /// flow. Endpoint is used for end-user authentication.
        /// </summary>
        string? AuthorizationEndpoint { get; }
        
        /// <summary>
        /// Gets the URI of the token endpoint used for authentication.
        /// </summary>
        string? TokenEndpoint { get; }
        /// <summary>
        /// Gets the endpoint URI used to retrieve user information from the identity provider.
        /// </summary>
        string? UserInfoEndpoint { get; }
        /// <summary>
        /// Gets the URI of the JSON Web Key Set (JWKS) endpoint used to retrieve public keys for token validation.
        /// </summary>
        /// <remarks>The JWKS endpoint provides the public keys necessary for verifying JSON Web Tokens
        /// (JWTs) issued by an authorization server. This property may be null if the endpoint is not configured or not
        /// applicable.</remarks>
        string? JwksUri { get; }

        /// <summary>
        /// Gets the endpoint URI used to terminate an authentication session with the identity provider.
        /// </summary>
        string? EndSessionEndpoint { get; }
    }

    internal record DiscoveryDocument(
        string Authority,
        string? Issuer,
        string? AuthorizationEndpoint,
        string? TokenEndpoint,
        string? UserInfoEndpoint,
        string? JwksUri,
        string? EndSessionEndpoint) : IDiscoveryDocument;
}

