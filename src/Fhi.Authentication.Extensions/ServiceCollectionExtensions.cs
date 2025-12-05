using Fhi.Authentication.OpenIdConnect;
using Fhi.Authentication.ClientCredentials;
using Fhi.Authentication.Tokens;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

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
        /// Add handler for certificate store keys. This is used for retrieving keys from the Windows certificate store.
        /// </summary>
        /// <param name="services">The service collection to add the handler to.</param>
        /// <param name="storeLocation">The certificate store location (CurrentUser or LocalMachine). Defaults to CurrentUser.</param>
        /// <returns>The updated service collection.</returns>
        public static IServiceCollection AddCertificateStoreKeyHandler(
            this IServiceCollection services,
            CertificateStoreLocation storeLocation = CertificateStoreLocation.CurrentUser)
        {
            services.AddTransient<ICertificateProvider>(_ => new StoreCertificateProvider(storeLocation));
            services.AddTransient<ICertificateKeyHandler, CertificateKeyHandler>();
            // Non-breaking addition: register certificate-to-JWK resolver for consumers that opt-in
            services.TryAddSingleton<ICertificateJwkResolver, CertificateJwkResolver>();
            return services;
        }
        
        /// <summary>
        /// Adds an in-memory discovery service for OpenID Connect authorities.
        /// Use IDiscoveryDocumentStore to retrieve discovery documents.
        /// </summary>
        /// <param name="services"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public static IServiceCollection AddInMemoryDiscoveryService(
           this IServiceCollection services,
           IEnumerable<DiscoveryDocumentStoreOptions> options)
        {
            foreach (var authority in options)
            {
                services.AddOptions<DiscoveryDocumentStoreOptions>(authority.Authority)
                  .Configure(opts =>
                  {
                      opts.Authority = authority.Authority;
                      opts.CacheDuration = authority.CacheDuration;
                  });
            }

            services.AddMemoryCache();
            services.AddHttpClient();
            services.AddSingleton<IDiscoveryDocumentStore, InMemoryDiscoveryDocumentStore>();

            return services;
        }

        /// <summary>
        /// Adds an in-memory discovery service to the specified service collection using the provided list of authority
        /// endpoints.
        /// </summary>
        /// <param name="services">The service collection to which the in-memory discovery service will be added.</param>
        /// <param name="authorities">A collection of authority endpoint URLs to be used for discovery. Each string should represent a valid
        /// authority URL.</param>
        /// <returns>The updated service collection with the in-memory discovery service registered.</returns>
        public static IServiceCollection AddInMemoryDiscoveryService(
               this IServiceCollection services,
               IEnumerable<string> authorities)
        {
            return services.AddInMemoryDiscoveryService(
                authorities.Select(a => new DiscoveryDocumentStoreOptions { Authority = a }));
        }

        /// <summary>
        /// Registers the secret store factory for dynamic secret resolution.
        /// The factory can create either file-based or certificate-based secret stores based on configuration.
        /// </summary>
        /// <param name="services">The service collection to configure.</param>
        /// <returns>The updated service collection.</returns>
        public static IServiceCollection AddSecretStoreFactory(this IServiceCollection services)
        {
            services.TryAddSingleton<ISecretStoreFactory, SecretStoreFactory>();
            services.TryAddSingleton<ICertificateJwkResolver, CertificateJwkResolver>();
            return services;
        }

        /// <summary>
        /// Configures a named secret store based on the provided options.
        /// The secret store type is automatically determined by which configuration properties are populated.
        /// </summary>
        /// <param name="services">The service collection to configure.</param>
        /// <param name="clientName">The name of the client configuration.</param>
        /// <param name="configure">A delegate to configure the secret store options.</param>
        /// <returns>The updated service collection.</returns>
        public static IServiceCollection AddSecretStore(
            this IServiceCollection services,
            string clientName,
            Action<SecretStoreOptions> configure)
        {
            if (string.IsNullOrEmpty(clientName))
                throw new ArgumentException("Client name cannot be null or empty", nameof(clientName));
            
            if (configure == null)
                throw new ArgumentNullException(nameof(configure));

            services.AddSecretStoreFactory();
            
            services.AddOptions<SecretStoreOptions>(clientName)
                .Configure(configure)
                .Validate(opts =>
                {
                    if (!string.IsNullOrEmpty(opts.CertificateThumbprint))
                    {
                        opts.SecretStoreType = SecretStoreType.Certificate;
                        return !string.IsNullOrEmpty(opts.ClientId);
                    }
                    else if (!string.IsNullOrEmpty(opts.PrivateJwk))
                    {
                        opts.SecretStoreType = SecretStoreType.File;
                        return true;
                    }
                    return false;
                }, "Either CertificateThumbprint (with ClientId) or PrivateJwk must be configured");

            return services;
        }
    }
}
