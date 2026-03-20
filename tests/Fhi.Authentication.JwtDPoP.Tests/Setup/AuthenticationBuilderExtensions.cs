using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;

namespace Fhi.Authentication.JwtDPoP.Tests.Setup
{
    internal static class AuthenticationBuilderExtensions
    {
        internal static AuthenticationBuilder AddJwtDpop(
            this AuthenticationBuilder builder,
            string authority = "http://authority",
            string audience = "api_audience",
            Action<JwtDPoPOptions>? configure = null)
        {
            builder.AddJwtDpop("DPoP", options =>
            {
                options.Authority = authority;
                options.Audience = audience;
                options.RequireHttpsMetadata = false;
                configure?.Invoke(options);
            });
            return builder;
        }

        internal static AuthenticationBuilder AddJwtBearer(
            this AuthenticationBuilder builder,
            string scheme = "Bearer",
            string authority = "http://authority",
            string audience = "api_audience",
            Action<JwtBearerOptions>? configure = null)
        {
            builder.AddJwtBearer(scheme, options =>
            {
                options.Authority = authority;
                options.Audience = audience;
                options.RequireHttpsMetadata = false;
                configure?.Invoke(options);
            });
            return builder;
        }
    }
}
