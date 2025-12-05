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
        private readonly ISecretStoreFactory? _secretStoreFactory;


        /// <inheritdoc/>
        public ClientCredentialsAssertionService(
            ILogger<ClientCredentialsAssertionService> logger,
            IOptionsMonitor<ClientAssertionOptions> clientAssertionOptions,
            IOptionsMonitor<ClientCredentialsClient> clientCredentialsClient,
            ISecretStoreFactory? secretStoreFactory = null)
        {
            _logger = logger;
            _clientAssertionOptions = clientAssertionOptions;
            _clientCredentialsClient = clientCredentialsClient;
            _secretStoreFactory = secretStoreFactory;
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

                // Resolve private JWK - try direct configuration first, then factory if available
                string privateJwk = clientAssertionOptions.PrivateJwk;

                // If PrivateJwk is not directly configured, try the factory (if registered)
                if (string.IsNullOrEmpty(privateJwk) && _secretStoreFactory != null)
                {
                    try
                    {
                        var secretStore = _secretStoreFactory.CreateSecretStore(clientName);
                        privateJwk = secretStore.GetPrivateKeyAsJwk();
                        _logger.LogInformation("Retrieved private JWK from secret store factory for {clientName}", clientName);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to retrieve private JWK from secret store factory for {clientName}", clientName);
                        return Task.FromResult<ClientAssertion?>(null);
                    }
                }

                if (string.IsNullOrEmpty(privateJwk))
                {
                    _logger.LogError("Could not resolve JWK for {clientName}. No PrivateJwk configured and factory unavailable or failed", clientName);
                    return Task.FromResult<ClientAssertion?>(null);
                }

                var jwt = ClientAssertionTokenHandler.CreateJwtToken(clientAssertionOptions.Issuer, client?.ClientId ?? "", privateJwk);
                return Task.FromResult<ClientAssertion?>(new ClientAssertion
                {
                    Type = clientAssertionOptions.ClientAssertionType,
                    Value = jwt
                });
            }

            if (client is null) _logger.LogError("Could not resolve options for client {clientName}", clientName);
            return Task.FromResult<ClientAssertion?>(null);
        }
    }
}