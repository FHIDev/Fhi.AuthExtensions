using Duende.AccessTokenManagement;
using Duende.IdentityModel;
using Duende.IdentityModel.Client;
using Fhi.Authentication.Tokens;
using Microsoft.Extensions.Options;

namespace WorkerService
{
    internal class ClientCredentialAssertionService : IClientAssertionService
    {
        private readonly IOptionsMonitor<ClientCredentialsClient> _options;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ClientConfiguration _clientConfiguration;

        public ClientCredentialAssertionService(
            IOptionsMonitor<ClientCredentialsClient> options,
            IOptions<ClientConfiguration> clientConfigurations,
            IHttpClientFactory httpClientFactory)
        {
            _options = options;
            _httpClientFactory = httpClientFactory;
            _clientConfiguration = clientConfigurations.Value;
        }
        public async Task<ClientAssertion?> GetClientAssertionAsync(string? clientName = null, TokenRequestParameters? parameters = null)
        {
            using var client = _httpClientFactory.CreateClient();
            var discovery = await client.GetDiscoveryDocumentAsync(_clientConfiguration.Authority);

            var jwt = ClientAssertionTokenHandler.CreateJwtToken(discovery.Issuer!, _clientConfiguration.ClientId, _clientConfiguration.Secret);

            return new ClientAssertion
            {
                Type = OidcConstants.ClientAssertionTypes.JwtBearer,
                Value = jwt
            };
        }
    }
}
