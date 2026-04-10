using Duende.AccessTokenManagement;
using Duende.IdentityModel;
using Duende.IdentityModel.Client;
using Fhi.Authentication.Tokens;
using Microsoft.Extensions.Options;

namespace BlazorApp.HelseId.Hosting.Authentication;

/// <summary>
/// Downstream service for client assertion.
/// </summary>
public class ClientAssertionService(
    IOptions<AuthenticationSettings> Options,
    IDiscoveryCache DiscoveryCache) : IClientAssertionService
{
    public async Task<ClientAssertion?> GetClientAssertionAsync(ClientCredentialsClientName? clientName = null, TokenRequestParameters? parameters = null, CancellationToken ct = default)
    {
        var discoveryDocument = await DiscoveryCache.GetAsync();

        if (discoveryDocument.IsError) throw new Exception(discoveryDocument.Error);

        var expiration = DateTime.UtcNow.AddSeconds(30);

        var clientAssertion = ClientAssertionTokenHandler.CreateJwtToken(
            discoveryDocument.Issuer!,
            Options.Value.ClientId,
            Options.Value.ClientSecret,
            expiration);

        return new ClientAssertion { Type = OidcConstants.ClientAssertionTypes.JwtBearer, Value = clientAssertion };
    }
}
