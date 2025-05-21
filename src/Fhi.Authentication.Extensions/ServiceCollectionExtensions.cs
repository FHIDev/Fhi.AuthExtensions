using Fhi.Authentication.Extensions;
using Fhi.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Refit;

namespace Fhi.Authentication
{
    /// <summary>
    /// Extensions for adding OpenIdConnect authentication services to the service collection.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Set default cookie options for OpenIdConnect. This is used to handle token expiration for downstream API calls.
        /// </summary>
        /// <param name="services">The service collection to add the authentication services to.</param>
        /// <returns>The updated service collection.</returns>
        public static IServiceCollection AddOpenIdConnectCookieOptions(this IServiceCollection services)
        {
            services.AddTransient<OpenIdConnectCookieEventsForApi>();
            services.AddTransient<ITokenService, DefaultTokenService>();
            services.AddSingleton<IPostConfigureOptions<CookieAuthenticationOptions>, OpenIdConnectCookieAuthenticationOptions>();

            return services;
        }

        /// <summary>
        /// Adds and configures a Refit client with client credentials token management.
        /// This method automatically handles OAuth 2.0 access token acquisition and renewal
        /// using the client credentials grant type for machine-to-machine authentication.
        /// </summary>
        /// <typeparam name="T">The type of the Refit client interface.</typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
        /// <param name="configuration">The application configuration, used to bind <see cref="RefitClientCredentialsOptions"/>.</param>
        /// <param name="configureRefitSettings">Optional action to configure Refit settings such as serialization options.</param>
        /// <param name="configureHttpClient">Optional action to configure the HttpClient, such as setting timeouts or additional headers.</param>
        /// <returns>The <see cref="IHttpClientBuilder"/> for further configuration of the HTTP client pipeline.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the required configuration section is missing or when required properties like TokenEndpoint or ClientId are not configured.
        /// </exception>
        /// <example>
        /// <code>
        /// services.AddRefitClientWithClientCredentials&lt;IMyApi&gt;(configuration);
        /// 
        /// // With custom configuration
        /// services.AddRefitClientWithClientCredentials&lt;IMyApi&gt;(configuration,
        ///     refitSettings => refitSettings.ContentSerializer = new SystemTextJsonContentSerializer(),
        ///     httpClient => httpClient.Timeout = TimeSpan.FromSeconds(30));
        /// </code>
        /// </example>
        public static IHttpClientBuilder AddRefitClientWithClientCredentials<T>(
            this IServiceCollection services,
            IConfiguration configuration,
            Action<RefitSettings>? configureRefitSettings = null,
            Action<HttpClient>? configureHttpClient = null)
            where T : class
        {
            // 1. Configure and validate options
            var options = ConfigureClientCredentialsOptions(services, configuration);
            
            // 2. Add required dependencies
            AddRequiredDependencies(services);
            
            // 3. Configure token management with JWK authentication
            ConfigureJwkBasedTokenManagement(services, options);
            
            // 4. Register and configure the Refit client
            return ConfigureRefitClient<T>(services, options, configureRefitSettings, configureHttpClient);
        }

        private static RefitClientCredentialsOptions ConfigureClientCredentialsOptions(
            IServiceCollection services, 
            IConfiguration configuration)
        {
            // Get the configuration section
            var configSection = configuration.GetSection(RefitClientCredentialsOptions.SectionName);
            
            // Register and validate options using the options pattern
            services.Configure<RefitClientCredentialsOptions>(RefitClientCredentialsOptions.SectionName, configSection);
            services.AddOptionsWithValidateOnStart<RefitClientCredentialsOptions>(RefitClientCredentialsOptions.SectionName)
                .ValidateDataAnnotations();

            // Retrieve and validate options
            var options = configSection.Get<RefitClientCredentialsOptions>()
                          ?? throw new InvalidOperationException(
                              $"Configuration section '{RefitClientCredentialsOptions.SectionName}' not found or could not be bound to RefitClientCredentialsOptions. " +
                              $"Please ensure the configuration section exists and contains valid values.");

            // Additional validation for immediate feedback
            ValidateRequiredOptions(options);
            
            return options;
        }

        private static void ValidateRequiredOptions(RefitClientCredentialsOptions options)
        {
            if (string.IsNullOrWhiteSpace(options.TokenEndpoint))
                throw new InvalidOperationException(
                    $"TokenEndpoint must be configured in '{RefitClientCredentialsOptions.SectionName}' section. " +
                    $"Please provide a valid OAuth 2.0 token endpoint URL.");
                    
            if (string.IsNullOrWhiteSpace(options.ClientId))
                throw new InvalidOperationException(
                    $"ClientId must be configured in '{RefitClientCredentialsOptions.SectionName}' section. " +
                    $"Please provide a valid OAuth 2.0 client identifier.");
                    
            if (string.IsNullOrWhiteSpace(options.ClientSecret))
                throw new InvalidOperationException(
                    $"ClientSecret (JWK) must be configured in '{RefitClientCredentialsOptions.SectionName}' section. " +
                    $"Please provide a valid JSON Web Key for private_key_jwt authentication.");
        }

        private static void AddRequiredDependencies(IServiceCollection services)
        {
            // Add distributed cache dependency required by AccessTokenManagement
            // Check if it's already registered to avoid duplicate registrations
            if (!services.Any(x => x.ServiceType == typeof(Microsoft.Extensions.Caching.Distributed.IDistributedCache)))
            {
                services.AddDistributedMemoryCache();
            }
        }

        private static void ConfigureJwkBasedTokenManagement(
            IServiceCollection services, 
            RefitClientCredentialsOptions options)
        {
            // Configure Duende AccessTokenManagement for client credentials with JWK authentication
            services
                .AddClientCredentialsTokenManagement()
                .AddClient(options.ClientName, client =>
                {
                    client.TokenEndpoint = options.TokenEndpoint;
                    client.ClientId = options.ClientId;
                    client.Scope = options.Scope;
                    // Note: ClientSecret is not set here as we use JWK-based client assertions instead
                });

            // Add discovery cache for efficient discovery document retrieval
            AddDiscoveryCache(services, options.TokenEndpoint);
            
            // Register the custom client assertion service for JWK-based private_key_jwt authentication
            AddJwkClientAssertionService(services, options);
        }

        private static void AddDiscoveryCache(IServiceCollection services, string tokenEndpoint)
        {
            services.AddSingleton<Duende.IdentityModel.Client.IDiscoveryCache>(provider =>
            {
                // Extract issuer URL from token endpoint 
                var tokenEndpointUri = new Uri(tokenEndpoint);
                var issuerUrl = $"{tokenEndpointUri.Scheme}://{tokenEndpointUri.Authority}";
                
                return new Duende.IdentityModel.Client.DiscoveryCache(issuerUrl);
            });
        }

        private static void AddJwkClientAssertionService(
            IServiceCollection services, 
            RefitClientCredentialsOptions options)
        {
            services.AddTransient<Duende.AccessTokenManagement.IClientAssertionService>(provider =>
            {
                var discoveryCache = provider.GetRequiredService<Duende.IdentityModel.Client.IDiscoveryCache>();
                return new RefitClientAssertionService(discoveryCache, options.ClientId, options.ClientSecret!);
            });
        }

        private static IHttpClientBuilder ConfigureRefitClient<T>(
            IServiceCollection services,
            RefitClientCredentialsOptions options,
            Action<RefitSettings>? configureRefitSettings,
            Action<HttpClient>? configureHttpClient)
            where T : class
        {
            // Configure Refit settings
            var refitSettings = new RefitSettings();
            configureRefitSettings?.Invoke(refitSettings);

            // Register and configure the Refit client
            return services
                .AddRefitClient<T>(refitSettings)
                .ConfigureHttpClient(client =>
                {
                    if (!string.IsNullOrWhiteSpace(options.ApiBaseUrl))
                    {
                        client.BaseAddress = new Uri(options.ApiBaseUrl);
                    }
                    configureHttpClient?.Invoke(client);
                })
                .AddClientCredentialsTokenHandler(options.ClientName);
        }
    }
}
