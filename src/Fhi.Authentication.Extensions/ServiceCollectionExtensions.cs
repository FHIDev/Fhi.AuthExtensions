using Fhi.Authentication.OpenIdConnect;
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
        /// Adds an in-memory discovery service for OpenID Connect authorities.
        /// Use IDiscoveryDocumentStore to retrieve discovery documents.
        /// </summary>
        /// <param name="services"></param>
        /// <param name="authorities"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public static IServiceCollection AddInMemoryDiscoveryService(
           this IServiceCollection services,
           IEnumerable<string> authorities)
        {
            var authorityList = authorities.Distinct().ToList();
            if (!authorityList.Any())
                throw new ArgumentException("At least one authority must be provided.", nameof(authorities));

            services.AddHttpClient();
            services.TryAddSingleton<IDiscoveryDocumentStore>(sp =>
            {
                var factory = sp.GetRequiredService<IHttpClientFactory>();
                return new InMemoryDiscoveryDocumentStore(factory, authorityList);
            });

            return services;
        }
    }
}
