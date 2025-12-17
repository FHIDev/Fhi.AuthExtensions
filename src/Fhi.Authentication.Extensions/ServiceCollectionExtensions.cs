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
            services.AddTransient<IPrivateKeyHandler, PrivateKeyHandler>();
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


    }
}
