using Duende.AccessTokenManagement.OpenIdConnect;
using Fhi.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.DependencyInjection;
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
            services.AddTransient<IUserTokenEndpointService, UserTokenEndpointService>();
            services.AddSingleton<IPostConfigureOptions<CookieAuthenticationOptions>, OpenIdConnectCookieAuthenticationOptions>();

            return services;
        }
    }
}
