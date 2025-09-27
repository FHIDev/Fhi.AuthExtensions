using Duende.AccessTokenManagement;
using Duende.IdentityModel;
using Duende.IdentityModel.Client;
using Fhi.Authentication.Tokens;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Fhi.Authentication.ClientCredentials
{
    /// <summary>
    /// Called from Duende.AccesstokenManagemnt accesstoken delegation handler to generates a client assertion for authenticating the client.
    /// When this service is addedd to the DI container, Duende.AccessTokenManagement will use this service to get a client assertion
    /// </summary>
    public class ClientCredentialsAssertionService : IClientAssertionService
    {
        private readonly ILogger<ClientCredentialsAssertionService> _logger;
        private readonly IOptionsMonitor<ClientCredentialsClient> _clientCredentialsClients;


        /// <inheritdoc/>
        public ClientCredentialsAssertionService(
            ILogger<ClientCredentialsAssertionService> logger,
            IOptionsMonitor<ClientCredentialsClient> clientCredentialsClients)
        {
            _logger = logger;
            _clientCredentialsClients = clientCredentialsClients;
        }

        /// <inheritdoc/>
        public Task<ClientAssertion?> GetClientAssertionAsync(string? clientName = null, TokenRequestParameters? parameters = null)
        {
            var clientOption = _clientCredentialsClients.Get(clientName);
            if (clientOption != null && clientOption.ClientSecret == null)
            {
                var issuer = ResolveIssuer(clientOption);
                if (string.IsNullOrEmpty(issuer))
                {
                    _logger.LogError("Could not resolve issuer for {clientName}. Missing parameter", clientName);
                    return Task.FromResult<ClientAssertion?>(null);
                }

                var jwk = ResolveJwk(clientOption);
                if (string.IsNullOrEmpty(jwk))
                {
                    _logger.LogError("Could not resolve JWK for {clientName}. Missing parameter", clientName);
                    return Task.FromResult<ClientAssertion?>(null);
                }

                var jwt = ClientAssertionTokenHandler.CreateJwtToken(issuer, clientOption.ClientId ?? "", jwk);
                return Task.FromResult<ClientAssertion?>(new ClientAssertion
                {
                    Type = OidcConstants.ClientAssertionTypes.JwtBearer,
                    Value = jwt
                });
            }

            _logger.LogError("Could not resolve options for client {clientName}", clientName);
            return Task.FromResult<ClientAssertion?>(null);
        }

        private static string? ResolveIssuer(ClientCredentialsClient? option)
        {
            var issuerKeyPair = option?.Parameters?.FirstOrDefault(x => x.Key == ClientCredentialParameter.Issuer);
            var issuer = issuerKeyPair?.Value;
            return issuer;
        }

        private static string? ResolveJwk(ClientCredentialsClient? option)
        {
            var issuerKeyPair = option?.Parameters?.FirstOrDefault(x => x.Key == ClientCredentialParameter.PrivateJwk);
            var issuer = issuerKeyPair?.Value;
            return issuer;
        }
    }
}
