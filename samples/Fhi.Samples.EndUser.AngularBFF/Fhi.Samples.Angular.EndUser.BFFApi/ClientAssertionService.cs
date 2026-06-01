using Duende.AccessTokenManagement;
using Duende.AccessTokenManagement.OpenIdConnect;
using Duende.IdentityModel;
using Duende.IdentityModel.Client;
using Fhi.Authentication.Tokens;
using Microsoft.Extensions.Options;

namespace Fhi.Samples.Angular.BFFApi
{
    public class ClientAssertionService(IOptions<UserTokenManagementOptions> Options) : IClientAssertionService
    {

        public Task<ClientAssertion?> GetClientAssertionAsync(ClientCredentialsClientName? clientName = null, TokenRequestParameters? parameters = null, CancellationToken ct = default)
        {
            var clientAssertion = ClientAssertionTokenHandler.CreateJwtToken(
               "https://localhost:5001",
               "interactive",
               Options.Value.DPoPJsonWebKey!);

            return Task.FromResult<ClientAssertion?>(new ClientAssertion { Type = OidcConstants.ClientAssertionTypes.JwtBearer, Value = clientAssertion });
        }
    }
}
