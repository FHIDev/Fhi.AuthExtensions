using Duende.AccessTokenManagement;
using Duende.AccessTokenManagement.DPoP;
using Fhi.Authentication.ClientCredentials;
using Fhi.Authentication.OpenIdConnect;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods for configuring client credentials authentication.
    /// </summary>
    /// <remarks>
    /// Which overload to use?
    /// <list type="bullet">
    /// <item><description><c>ISecretStore</c> → Multi-environment (dev/prod) with DI - RECOMMENDED</description></item>
    /// <item><description><c>SharedSecret</c> → Simple client_secret auth</description></item>
    /// <item><description><c>PrivateJwk</c> → Direct JWK, dev/testing</description></item>
    /// <item><description><c>CertificateOptions</c> → Explicit cert control, single environment</description></item>
    /// </list>
    /// </remarks>
    public static class ClientCredentialsExtensions
    {
        /// <summary>
        /// Helper method to configure ClientCredentialsClient with common settings.
        /// Reduces code duplication across multiple overloads.
        /// </summary>
        private static OptionsBuilder<ClientCredentialsClient> ConfigureClientCredentialsClient(
            IServiceCollection services,
            string optionName,
            string authority,
            string clientId,
            string? scope,
            DPoPProofKey? dPoPKey)
        {
            return services
                .AddOptions<ClientCredentialsClient>(optionName)
                .Configure<IDiscoveryDocumentStore>((options, discoveryStore) =>
                {
                    var discoveryDocument = discoveryStore.Get(authority);
                    options.TokenEndpoint = discoveryDocument?.TokenEndpoint is not null
                        ? new Uri(discoveryDocument.TokenEndpoint)
                        : null;
                    options.ClientId = ClientId.Parse(clientId);

                    if (!string.IsNullOrEmpty(scope))
                        options.Scope = Scope.Parse(scope);

                    if (dPoPKey is not null)
                        options.DPoPJsonWebKey = dPoPKey;
                });
        }
        /// <summary>
        /// Configures OAuth2 client_secret authentication.
        /// </summary>
        /// <param name="services">The service collection to configure.</param>
        /// <param name="optionName">Unique name for this client configuration.</param>
        /// <param name="authority">The OAuth2/OIDC authority URL (e.g., "https://login.example.com").</param>
        /// <param name="clientId">The client identifier registered with the authority.</param>
        /// <param name="sharedSecret">The shared secret for client authentication.</param>
        /// <param name="scope">Optional. The OAuth2 scope(s) to request (space-separated).</param>
        /// <param name="dPoPKey">Optional. DPoP proof key for Demonstrating Proof-of-Possession.</param>
        /// <returns>A <see cref="ClientCredentialsOptionBuilder"/> for further configuration.</returns>
        public static ClientCredentialsOptionBuilder AddClientCredentialsClientOptions(
            this IServiceCollection services,
            string optionName,
            string authority,
            string clientId,
            SharedSecret sharedSecret,
            string? scope = null,
            DPoPProofKey? dPoPKey = null)
        {
            var option = ConfigureClientCredentialsClient(services, optionName, authority, clientId, scope, dPoPKey)
                .Configure<IDiscoveryDocumentStore>((options, discoveryStore) =>
                {
                    options.ClientSecret = ClientSecret.Parse(sharedSecret);
                });

            return new ClientCredentialsOptionBuilder(optionName, services, option);
        }

        /// <summary>
        /// Configures JWT authentication with direct JWK (resolved at startup).
        /// </summary>
        /// <param name="services">The service collection to configure.</param>
        /// <param name="optionName">Unique name for this client configuration.</param>
        /// <param name="authority">The OAuth2/OIDC authority URL (e.g., "https://login.example.com").</param>
        /// <param name="clientId">The client identifier registered with the authority.</param>
        /// <param name="privateJwk">The private JWK for signing client assertions.</param>
        /// <param name="scope">Optional. The OAuth2 scope(s) to request (space-separated).</param>
        /// <param name="dPoPKey">Optional. DPoP proof key for Demonstrating Proof-of-Possession.</param>
        /// <returns>A <see cref="ClientCredentialsOptionBuilder"/> for further configuration.</returns>
        public static ClientCredentialsOptionBuilder AddClientCredentialsClientOptions(
            this IServiceCollection services,
            string optionName,
            string authority,
            string clientId,
            PrivateJwk privateJwk,
            string? scope = null,
            DPoPProofKey? dPoPKey = null)
        {
            services.TryAddTransient<IClientAssertionService, ClientCredentialsAssertionService>();

            var clientAssertionBuilder = services
                .AddOptions<ClientAssertionOptions>(optionName)
                .Configure<IDiscoveryDocumentStore>((options, discoveryStore) =>
                {
                    var discoveryDocument = discoveryStore.Get(authority);
                    options.Issuer = discoveryDocument?.Issuer ?? string.Empty;
                    options.PrivateJwk = privateJwk;
                });

            var clientCredentialsBuilder = ConfigureClientCredentialsClient(services, optionName, authority, clientId, scope, dPoPKey);

            return new ClientCredentialsOptionBuilder(optionName, services, clientCredentialsBuilder, clientAssertionBuilder);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="PrivateJwkKeyHandler"></typeparam>
        /// <param name="services"></param>
        /// <param name="optionName"></param>
        /// <param name="authority"></param>
        /// <param name="clientId"></param>
        /// <param name="scope"></param>
        /// <param name="dPoPKey"></param>
        /// <returns></returns>
        public static ClientCredentialsOptionBuilder AddClientCredentialsClientOptions<PrivateJwkKeyHandler>(
          this IServiceCollection services,
          string optionName,
          string authority,
          string clientId,
          string? scope = null,
          DPoPProofKey? dPoPKey = null)
        {
            services.TryAddTransient<IClientAssertionService, ClientCredentialsAssertionService>();

            var clientAssertionBuilder = services
                .AddOptions<ClientAssertionOptions>(optionName)
                .Configure<IDiscoveryDocumentStore>((options, discoveryStore) =>
                {
                    var discoveryDocument = discoveryStore.Get(authority);
                    options.Issuer = discoveryDocument?.Issuer ?? string.Empty;
                });

            var clientCredentialsBuilder = ConfigureClientCredentialsClient(services, optionName, authority, clientId, scope, dPoPKey);

            return new ClientCredentialsOptionBuilder(optionName, services, clientCredentialsBuilder, clientAssertionBuilder);
        }


        /// <summary>
        /// Registers a named <see cref="HttpClient"/> preconfigured with a token delegation handler.
        /// </summary>
        /// <param name="builder">
        /// The <see cref="ClientCredentialsOptionBuilder"/> used to identify the client credentials configuration.
        /// </param>
        /// <param name="configureHttpClient">An optional delegate to configure the <see cref="HttpClient"/>.</param>
        /// <returns>
        /// The same <see cref="ClientCredentialsOptionBuilder"/> for fluent chaining.
        /// </returns>
        public static IHttpClientBuilder AddClientCredentialsHttpClient(
            this ClientCredentialsOptionBuilder builder,
            Action<HttpClient>? configureHttpClient)
        {
            return builder.Services.AddClientCredentialsHttpClient(
                builder.Name,
                ClientCredentialsClientName.Parse(builder.ClientCredentialsClientOptions.Name),
                configureHttpClient);
        }
    }

    /// <summary>
    /// Represents a fluent builder for configuring client credential options and related services.
    /// </summary>
    /// <param name="Name">The unique name of the configuration.</param>
    /// <param name="Services">The service collection being configured.</param>
    /// <param name="ClientCredentialsClientOptions">The options builder for <see cref="ClientCredentialsClient"/>.</param>
    /// <param name="ClientAssertionOptions">Optional. The options builder for <see cref="ClientAssertionOptions"/>.</param>
    public record ClientCredentialsOptionBuilder(
        string Name,
        IServiceCollection Services,
        OptionsBuilder<ClientCredentialsClient> ClientCredentialsClientOptions,
        OptionsBuilder<ClientAssertionOptions>? ClientAssertionOptions = null);
}
