using Duende.AccessTokenManagement;
using Duende.IdentityModel;
using Duende.IdentityModel.Client;
using Fhi.Authentication.Tokens;
using Microsoft.Extensions.Options;

namespace BlazorInteractiveServer.Hosting.Authentication
{
    /// <summary>
    /// Downstream service for client assertion.
    /// TODO: handle multiple downstream clients e.g. clientCredentials and token exchange
    /// </summary>
    public class ClientAssertionService(
        IOptionsSnapshot<AuthenticationSettings> Options,
        IDiscoveryCache DiscoveryCache) : IClientAssertionService
    {
        public async Task<ClientAssertion?> GetClientAssertionAsync(ClientCredentialsClientName? clientName = null, TokenRequestParameters? parameters = null, CancellationToken ct = default)
        {
            //Method to get different downstream http clients
            //var client = Options.Get(clientName);

            var discoveryDocument = await DiscoveryCache.GetAsync();

            if (discoveryDocument.IsError) throw new Exception(discoveryDocument.Error);

            var clientAssertion = ClientAssertionTokenHandler.CreateJwtToken(discoveryDocument.Issuer!, Options.Value.ClientId, Options.Value.ClientSecret);

            return new ClientAssertion { Type = OidcConstants.ClientAssertionTypes.JwtBearer, Value = clientAssertion };
        }
    }
}
