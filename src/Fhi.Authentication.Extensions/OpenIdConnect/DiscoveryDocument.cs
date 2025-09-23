namespace Fhi.Authentication.OpenIdConnect
{
    public interface IDiscoveryDocument
    {
        string? Authority { get; }
        string? Issuer { get; }
        string? AuthorizationEndpoint { get; }
        string? TokenEndpoint { get; }
        string? UserInfoEndpoint { get; }
        string? JwksUri { get; }
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

