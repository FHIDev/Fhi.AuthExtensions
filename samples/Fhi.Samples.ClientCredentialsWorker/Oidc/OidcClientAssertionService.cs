using Duende.AccessTokenManagement;
using Duende.IdentityModel;
using Duende.IdentityModel.Client;
using Fhi.Authentication.Tokens;
using Fhi.Samples.WorkerServiceMultipleClients.Oidc;
using Microsoft.Extensions.Options;

namespace Fhi.Samples.ClientCredentialsWorkers.Oidc
{

    /// <summary>
    /// Generates a client assertion using a JWK for authentication towards an OIDC provider.
    /// </summary>
    public class OidcClientAssertionService : IClientAssertionService
    {
        private readonly ILogger<OidcClientAssertionService> _logger;
        private readonly IOptionsMonitor<ClientCredentialsClient> _clientCredentialsClients;

        public OidcClientAssertionService(
            ILogger<OidcClientAssertionService> logger,
            IOptionsMonitor<ClientCredentialsClient> clientCredentialsClients)
        {
            _logger = logger;
            _clientCredentialsClients = clientCredentialsClients;
        }

        public Task<ClientAssertion?> GetClientAssertionAsync(string? clientName = null, TokenRequestParameters? parameters = null)
        {
            var clientOption = _clientCredentialsClients.Get(clientName);
            if (clientOption != null && clientOption.ClientSecret == null)
            {
                var issuer = ResolveIssuer(clientOption);
                var jwk = ResolveJwk(clientOption);
                var jwt = ClientAssertionTokenHandler.CreateJwtToken(issuer, clientOption.ClientId ?? "", jwk);
                return Task.FromResult<ClientAssertion?>(new ClientAssertion
                {
                    Type = OidcConstants.ClientAssertionTypes.JwtBearer,
                    Value = jwt
                });
            }

            _logger.LogWarning("Could not resolve options for client {clientName}", clientName);
            return Task.FromResult<ClientAssertion?>(null);
        }

        private static string ResolveIssuer(ClientCredentialsClient? option)
        {
            var issuerKeyPair = option?.Parameters?.FirstOrDefault(x => x.Key == ClientCredentialParameter.Issuer);
            var issuer = issuerKeyPair?.Value;
            return issuer ?? "";
        }

        private static string ResolveJwk(ClientCredentialsClient? option)
        {
            var issuerKeyPair = option?.Parameters?.FirstOrDefault(x => x.Key == ClientCredentialParameter.PrivateJwk);
            var issuer = issuerKeyPair?.Value;
            return issuer ?? "";
        }
    }
}