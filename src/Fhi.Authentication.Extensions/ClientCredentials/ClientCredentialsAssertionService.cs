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
        private readonly TimeProvider _timeProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClientCredentialsAssertionService"/> class.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        /// <param name="clientAssertionOptions">The client assertion options.</param>
        /// <param name="clientCredentialsClient">The client credentials client options.</param>
        public ClientCredentialsAssertionService(
            ILogger<ClientCredentialsAssertionService> logger,
            IOptionsMonitor<ClientAssertionOptions> clientAssertionOptions,
            IOptionsMonitor<ClientCredentialsClient> clientCredentialsClient)
            : this(logger, clientAssertionOptions, clientCredentialsClient, TimeProvider.System) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ClientCredentialsAssertionService"/> class.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        /// <param name="clientAssertionOptions">The client assertion options.</param>
        /// <param name="clientCredentialsClient">The client credentials client options.</param>
        /// <param name="timeProvider">The time provider for generating expiration timestamps.</param>
        public ClientCredentialsAssertionService(
            ILogger<ClientCredentialsAssertionService> logger,
            IOptionsMonitor<ClientAssertionOptions> clientAssertionOptions,
            IOptionsMonitor<ClientCredentialsClient> clientCredentialsClient,
            TimeProvider timeProvider)
        {
            _logger = logger;
            _clientAssertionOptions = clientAssertionOptions;
            _clientCredentialsClient = clientCredentialsClient;
            _timeProvider = timeProvider;
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

                string privateJwk = clientAssertionOptions.PrivateJwk;
                var utcNow = _timeProvider.GetUtcNow();
                var expiration = utcNow.AddSeconds(clientAssertionOptions.ExpirationSeconds).UtcDateTime;
                var jwt = ClientAssertionTokenHandler.CreateJwtToken(clientAssertionOptions.Issuer, client.ClientId ?? "", privateJwk, expiration, utcNow: utcNow);
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