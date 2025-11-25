using Duende.AccessTokenManagement;
using Duende.IdentityModel.Client;
using Fhi.Authentication.Tokens;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Fhi.Authentication.ClientCredentials
{

    /// <summary>
    /// Called from Duende.AccesstokenManagemnt accesstoken delegation handler to generate a client assertion for authenticating the client.
    /// When this service is added to the DI container, Duende.AccessTokenManagement will use this service to get a client assertion
    /// </summary>
    public class ClientCredentialsAssertionService : IClientAssertionService
    {
        private readonly ILogger<ClientCredentialsAssertionService> _logger;
        private readonly IOptionsMonitor<ClientAssertionOptions> _clientAssertionOptions;
        private readonly IOptionsMonitor<ClientCredentialsClient> _clientCredentialsClient;
        private readonly ICertificateKeyHandler? _certificateKeyHandler;

        /// <inheritdoc/>
        public ClientCredentialsAssertionService(
            ILogger<ClientCredentialsAssertionService> logger,
            IOptionsMonitor<ClientAssertionOptions> clientAssertionOptions,
            IOptionsMonitor<ClientCredentialsClient> clientCredentialsClient,
            ICertificateKeyHandler? certificateKeyHandler = null)
        {
            _logger = logger;
            _clientAssertionOptions = clientAssertionOptions;
            _clientCredentialsClient = clientCredentialsClient;
            _certificateKeyHandler = certificateKeyHandler;
        }

        /// <inheritdoc/>
        public Task<ClientAssertion?> GetClientAssertionAsync(ClientCredentialsClientName? clientName = null, TokenRequestParameters? parameters = null, CancellationToken ct = default)
        {
            var client = _clientCredentialsClient.Get(clientName);
            if (client != null && client.ClientSecret == null)
            {
                var clientAssertionOptions = _clientAssertionOptions.Get(clientName);
                if (string.IsNullOrEmpty(clientAssertionOptions.Issuer))
                {
                    _logger.LogError("Could not resolve issuer for {clientName}. Missing parameter", clientName);
                    return Task.FromResult<ClientAssertion?>(null);
                }
                
                // IF JWK isnt nul or empty, do the thing with JWK and return
                if (!string.IsNullOrEmpty(clientAssertionOptions.PrivateJwk))
                {
                    var jwt = ClientAssertionTokenHandler.CreateJwtToken(clientAssertionOptions.Issuer, client.ClientId ?? "", clientAssertionOptions.PrivateJwk);
                    return Task.FromResult<ClientAssertion?>(new ClientAssertion
                    {
                        Type = clientAssertionOptions.ClientAssertionType,
                        Value = jwt
                    });
                }
                
                // IF CertificateThumbprint isnt null or empty, do the thing with certificate and return
                if (_certificateKeyHandler != null && !string.IsNullOrEmpty(clientAssertionOptions.CertificateThumbprint))
                {
                    var jwk =  _certificateKeyHandler.GetPrivateKeyAsJwk(clientAssertionOptions.CertificateThumbprint);
                    var jwt = ClientAssertionTokenHandler.CreateJwtToken(clientAssertionOptions.Issuer, client.ClientId ?? "", jwk);
                    return Task.FromResult<ClientAssertion?>(new ClientAssertion
                    {
                        Type = clientAssertionOptions.ClientAssertionType,
                        Value = jwt
                    });
                }
            }
            _logger.LogError("Could not resolve options for client {clientName}", clientName);
            return Task.FromResult<ClientAssertion?>(null);
        }
    }
}
