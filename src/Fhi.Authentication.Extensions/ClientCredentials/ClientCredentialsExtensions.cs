using Duende.AccessTokenManagement;
using Duende.AccessTokenManagement.DPoP;
using Fhi.Authentication;
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
    /// <strong>Which overload to use?</strong>
    /// <list type="bullet">
    /// <item><description><c>AddClientCredentialsClientOptionsWithSecretStore</c> → Multi-environment (dev/prod) with auto-detection ⭐ RECOMMENDED</description></item>
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
        /// <remarks>
        /// <strong>Use when:</strong> Simple auth, internal APIs, or JWT not required.
        /// <strong>Security:</strong> Less secure than JWT-based auth (PrivateJwk/Certificate).
        /// </remarks>
        /// <param name="services">See class remarks for common parameters.</param>
        /// <param name="optionName">See class remarks for common parameters.</param>
        /// <param name="authority">See class remarks for common parameters.</param>
        /// <param name="clientId">See class remarks for common parameters.</param>
        /// <param name="sharedSecret">The shared secret for client authentication.</param>
        /// <param name="scope">See class remarks for common parameters.</param>
        /// <param name="dPoPKey">See class remarks for common parameters.</param>
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
                    // Add the shared secret (only difference from base config)
                    options.ClientSecret = ClientSecret.Parse(sharedSecret);
                });
            
            return new ClientCredentialsOptionBuilder(optionName, services, option);
        }

        /// <summary>
        /// Configures JWT authentication with direct JWK (resolved at startup).
        /// </summary>
        /// <remarks>
        /// <strong>Use when:</strong> Dev/testing with user secrets, or simple prod with static JWK.
        /// <strong>Alternatives:</strong> Use <c>AddClientCredentialsClientOptionsWithSecretStore</c> for environment-based config (dev vs prod).
        /// </remarks>
        /// <param name="services">See class remarks for common parameters.</param>
        /// <param name="optionName">See class remarks for common parameters.</param>
        /// <param name="authority">See class remarks for common parameters.</param>
        /// <param name="clientId">See class remarks for common parameters.</param>
        /// <param name="privateJwk">The private JWK for signing client assertions.</param>
        /// <param name="scope">See class remarks for common parameters.</param>
        /// <param name="dPoPKey">See class remarks for common parameters.</param>
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
        /// Configures JWT authentication with certificate (resolved at startup from cert store or PEM).
        /// </summary>
        /// <remarks>
        /// <strong>Use when:</strong> Production with Windows cert store, explicit cert control, single environment.
        /// <strong>Alternatives:</strong> Use <c>AddClientCredentialsClientOptionsWithSecretStore</c> to switch between cert (prod) and JWK (dev).
        /// </remarks>
        /// <param name="services">See class remarks for common parameters.</param>
        /// <param name="optionName">See class remarks for common parameters.</param>
        /// <param name="authority">See class remarks for common parameters.</param>
        /// <param name="clientId">See class remarks for common parameters.</param>
        /// <param name="certificate">Certificate options (thumbprint or PEM content).</param>
        /// <param name="scope">See class remarks for common parameters.</param>
        /// <param name="dPoPKey">See class remarks for common parameters.</param>
        /// <returns>A <see cref="ClientCredentialsOptionBuilder"/> for further configuration.</returns>
        public static ClientCredentialsOptionBuilder AddClientCredentialsClientOptions(
            this IServiceCollection services,
            string optionName,
            string authority,
            string clientId,
            CertificateOptions certificate,
            string? scope = null,
            DPoPProofKey? dPoPKey = null)
        {
            services.TryAddTransient<IClientAssertionService, ClientCredentialsAssertionService>();
            services.TryAddSingleton<ICertificateJwkResolver, CertificateJwkResolver>();

            // Configure ClientAssertionOptions using certificate→JWK resolution
            var clientAssertionBuilder = services
                .AddOptions<ClientAssertionOptions>(optionName)
                .Configure<IDiscoveryDocumentStore, ICertificateJwkResolver>((options, discoveryStore, resolver) =>
                {
                    var discoveryDocument = discoveryStore.Get(authority);
                    options.Issuer = discoveryDocument?.Issuer ?? string.Empty;
                    var jwkJson = resolver.ResolveToJwk(certificate);
                    options.PrivateJwk = PrivateJwk.ParseFromJson(jwkJson);
                });

            var clientCredentialsBuilder = ConfigureClientCredentialsClient(services, optionName, authority, clientId, scope, dPoPKey);

            return new ClientCredentialsOptionBuilder(optionName, services, clientCredentialsBuilder, clientAssertionBuilder);
        }

        /// <summary>
        /// Configures JWT authentication with runtime auto-detection (certificate or JWK based on config). ⭐ RECOMMENDED
        /// </summary>
        /// <remarks>
        /// <strong>Use when:</strong> Multi-environment (dev uses JWK, prod uses certificate), config-driven secret selection.
        /// <strong>Auto-detection:</strong> CertificateThumbprint → cert store, PrivateJwk → file/env var.
        /// <strong>Example:</strong> Dev: <c>"PrivateJwk": "{...}"</c>, Prod: <c>"Certificate": {"Thumbprint": "ABC..."}</c>
        /// </remarks>
        /// <param name="services">See class remarks for common parameters.</param>
        /// <param name="optionName">See class remarks for common parameters.</param>
        /// <param name="authority">See class remarks for common parameters.</param>
        /// <param name="clientId">See class remarks for common parameters.</param>
        /// <param name="configureSecretStore">Delegate to configure secret store (populate PrivateJwk OR CertificateThumbprint+ClientId).</param>
        /// <param name="scope">See class remarks for common parameters.</param>
        /// <param name="dPoPKey">See class remarks for common parameters.</param>
        /// <returns>A <see cref="ClientCredentialsOptionBuilder"/> for further configuration.</returns>
        public static ClientCredentialsOptionBuilder AddClientCredentialsClientOptionsWithSecretStore(
            this IServiceCollection services,
            string optionName,
            string authority,
            string clientId,
            Action<SecretStoreOptions> configureSecretStore,
            string? scope = null,
            DPoPProofKey? dPoPKey = null)
        {
            if (string.IsNullOrEmpty(optionName))
                throw new ArgumentException("Option name cannot be null or empty", nameof(optionName));
            
            if (configureSecretStore == null)
                throw new ArgumentNullException(nameof(configureSecretStore));

            services.TryAddTransient<IClientAssertionService, ClientCredentialsAssertionService>();
            
            services.AddSecretStore(optionName, configureSecretStore);

            // Configure ClientAssertionOptions - leave PrivateJwk empty so factory is used
            var clientAssertionBuilder = services
                .AddOptions<ClientAssertionOptions>(optionName)
                .Configure<IDiscoveryDocumentStore>((options, discoveryStore) =>
                {
                    var discoveryDocument = discoveryStore.Get(authority);
                    options.Issuer = discoveryDocument?.Issuer ?? string.Empty;
                    // PrivateJwk is left empty - factory will be used automatically
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
