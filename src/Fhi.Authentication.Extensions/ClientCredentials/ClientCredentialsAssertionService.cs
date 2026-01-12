using Duende.AccessTokenManagement;
using Duende.IdentityModel.Client;
using Fhi.Authentication.Tokens;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Fhi.Authentication.ClientCredentials
{

    /// <summary>
    /// Called from Duende.AccessTokenManagement accesstoken delegation handler to generate a client assertion for authenticating the client.
    /// When this service is added to the DI container, Duende.AccessTokenManagement will use this service to get a client assertion.
    /// </summary>
    public class ClientCredentialsAssertionService : IClientAssertionService
    {
        private readonly ILogger<ClientCredentialsAssertionService> _logger;
        private readonly IOptionsMonitor<ClientAssertionOptions> _clientAssertionOptions;
        private readonly IOptionsMonitor<ClientCredentialsClient> _clientCredentialsClient;
        private readonly ISecretStore? _secretStore;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClientCredentialsAssertionService"/> class.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        /// <param name="clientAssertionOptions">The client assertion options.</param>
        /// <param name="clientCredentialsClient">The client credentials client options.</param>
        /// <param name="secretStore">Optional. The secret store for retrieving private JWK at runtime.</param>
        public ClientCredentialsAssertionService(
            ILogger<ClientCredentialsAssertionService> logger,
            IOptionsMonitor<ClientAssertionOptions> clientAssertionOptions,
            IOptionsMonitor<ClientCredentialsClient> clientCredentialsClient,
            ISecretStore? secretStore = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _clientAssertionOptions = clientAssertionOptions ?? throw new ArgumentNullException(nameof(clientAssertionOptions));
            _clientCredentialsClient = clientCredentialsClient ?? throw new ArgumentNullException(nameof(clientCredentialsClient));
            _secretStore = secretStore;
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

                // Resolve private JWK - try direct configuration first, then secret store if available
                string privateJwk = clientAssertionOptions.PrivateJwk;

                // If PrivateJwk is not directly configured, try the secret store (if registered)
                if (string.IsNullOrEmpty(privateJwk) && _secretStore != null)
                {
                    try
                    {
                        var jwk = _secretStore.GetPrivateJwk();
                        privateJwk = jwk;
                        _logger.LogDebug("Retrieved private JWK from secret store for {clientName}", clientName);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to retrieve private JWK from secret store for {clientName}", clientName);
                        return Task.FromResult<ClientAssertion?>(null);
                    }
                }

                if (string.IsNullOrEmpty(privateJwk))
                {
                    _logger.LogError("Could not resolve JWK for {clientName}. No PrivateJwk configured and secret store unavailable or failed", clientName);
                    return Task.FromResult<ClientAssertion?>(null);
                }
                
                if (clientAssertionOptions.ExpirationSeconds < 1)
                {
                    _logger.LogError("Invalid ExpirationSeconds ({ExpirationSeconds}) for {clientName}. Must be greater than or equal to 0", 
                        clientAssertionOptions.ExpirationSeconds, clientName);
                    return Task.FromResult<ClientAssertion?>(null);
                }

                var expiration = DateTime.UtcNow.AddSeconds(clientAssertionOptions.ExpirationSeconds);
                var jwt = ClientAssertionTokenHandler.CreateJwtToken(clientAssertionOptions.Issuer, client.ClientId ?? "", privateJwk, expiration);
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