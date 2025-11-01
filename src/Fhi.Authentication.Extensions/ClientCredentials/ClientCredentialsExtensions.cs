using Duende.AccessTokenManagement;
using Duende.AccessTokenManagement.DPoP;
using Fhi.Authentication.ClientCredentials;
using Fhi.Authentication.OpenIdConnect;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Provides extension methods for configuring client credentials within an <see cref="IServiceCollection"/>.
    /// </summary>
    /// <remarks>
    /// Includes helpers for registering client credential options and configuring HTTP clients 
    /// with automatic token acquisition and delegation.
    /// </remarks>
    public static class ClientCredentialsExtensions
    {
        /// <summary>
        /// Registers client credentials configuration for a specific named client using a shared secret.
        /// </summary>
        /// <remarks>
        /// Retrieves the discovery document from the specified authority to determine the token endpoint, 
        /// and configures the client credentials with the provided client ID, secret, and optional parameters.
        /// </remarks>
        /// <param name="services">The service collection to configure.</param>
        /// <param name="optionName">The name of the client configuration. Must not be null or empty.</param>
        /// <param name="authority">The authority URL used to retrieve the discovery document.</param>
        /// <param name="clientId">The client identifier used for authentication.</param>
        /// <param name="sharedSecret">The shared secret used for client authentication.</param>
        /// <param name="scope">Optional. The requested access token scope.</param>
        /// <param name="dPoPKey">Optional. The DPoP key (JSON Web Key) for proof-of-possession tokens.</param>
        /// <returns>
        /// A <see cref="ClientCredentialsOptionBuilder"/> that can be used to chain further configuration.
        /// </returns>
        public static ClientCredentialsOptionBuilder AddClientCredentialsClientOptions(
            this IServiceCollection services,
            string optionName,
            string authority,
            string clientId,
            SharedSecret sharedSecret,
            string? scope = null,
            DPoPProofKey? dPoPKey = null)
        {
            var option = services
               .AddOptions<ClientCredentialsClient>(optionName)
               .Configure<IDiscoveryDocumentStore>((options, discoveryStore) =>
               {
                   var discoveryDocument = discoveryStore.Get(authority);

                   options.TokenEndpoint = discoveryDocument?.TokenEndpoint is not null
                       ? new Uri(discoveryDocument.TokenEndpoint)
                       : null;

                   options.ClientId = ClientId.Parse(clientId);
                   options.ClientSecret = ClientSecret.Parse(sharedSecret);

                   if (!string.IsNullOrEmpty(scope))
                       options.Scope = Scope.Parse(scope);

                   if (dPoPKey is not null)
                       options.DPoPJsonWebKey = dPoPKey;
               });
            return new ClientCredentialsOptionBuilder(optionName, services, option);
        }

        /// <summary>
        /// Registers client credentials configuration for a specific named client using a private JWK-based assertion.
        /// </summary>
        /// <remarks>
        /// Configures both <see cref="ClientAssertionOptions"/> and <see cref="ClientCredentialsClient"/> 
        /// for use with private key JWT authentication. The authority’s discovery document is used 
        /// to resolve endpoints and issuer details.
        /// </remarks>
        /// <param name="services">The service collection to configure.</param>
        /// <param name="optionName">The name of the client configuration. Must not be null or empty.</param>
        /// <param name="authority">The authority URL used to retrieve the discovery document.</param>
        /// <param name="clientId">The client identifier used for authentication.</param>
        /// <param name="privateJwk">The private JWK used to sign the client assertion.</param>
        /// <param name="scope">Optional. The requested access token scope.</param>
        /// <param name="dPoPKey">Optional. The DPoP key for proof-of-possession tokens.</param>
        /// <returns>
        /// A <see cref="ClientCredentialsOptionBuilder"/> that can be used to chain further configuration.
        /// </returns>
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

            var clientCredentialsBuilder = services
                .AddOptions<ClientCredentialsClient>(optionName)
                .Configure<IDiscoveryDocumentStore>((options, discoveryStore) =>
                {
                    var discoveryDocument = discoveryStore.Get(authority);
                    options.TokenEndpoint = discoveryDocument?.TokenEndpoint is not null ? new Uri(discoveryDocument.TokenEndpoint) : null;
                    options.ClientId = ClientId.Parse(clientId);
                    if (!string.IsNullOrEmpty(scope))
                        options.Scope = Scope.Parse(scope);
                    if (dPoPKey is not null)
                        options.DPoPJsonWebKey = dPoPKey;
                });

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
