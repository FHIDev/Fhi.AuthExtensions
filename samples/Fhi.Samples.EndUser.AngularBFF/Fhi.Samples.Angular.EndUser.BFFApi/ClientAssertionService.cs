using Duende.AccessTokenManagement;
using Duende.IdentityModel;
using Duende.IdentityModel.Client;
using Fhi.Authentication.Tokens;
using Microsoft.Extensions.Options;

namespace Fhi.Samples.Angular.BFFApi
{
    internal class ClientAssertionService(IOptions<AuthenticationSettings> AuthOptions) : IClientAssertionService
    {

        public Task<ClientAssertion?> GetClientAssertionAsync(ClientCredentialsClientName? clientName = null, TokenRequestParameters? parameters = null, CancellationToken ct = default)
        {
            var clientAssertion = ClientAssertionTokenHandler.CreateJwtToken(
               AuthOptions.Value.Authority,
               AuthOptions.Value.ClientId,
               AuthOptions.Value.ClientSecret);

            return Task.FromResult<ClientAssertion?>(new ClientAssertion { Type = OidcConstants.ClientAssertionTypes.JwtBearer, Value = clientAssertion });
        }
    }
}
