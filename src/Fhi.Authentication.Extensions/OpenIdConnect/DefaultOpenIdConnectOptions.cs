using Duende.IdentityModel;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Fhi.Authentication.OpenIdConnect
{
    /// <summary>
    /// Set default options for OpenIdConnect authentication.
    /// </summary>
    public class DefaultOpenIdConnectOptions : IPostConfigureOptions<OpenIdConnectOptions>
    {
        /// <inheritdoc/>
        public void PostConfigure(string? name, OpenIdConnectOptions options)
        {
            options.SaveTokens = true;
            options.GetClaimsFromUserInfoEndpoint = true;
            options.MapInboundClaims = false;
            options.TokenValidationParameters ??= new TokenValidationParameters();
            if (string.IsNullOrEmpty(options.TokenValidationParameters.NameClaimType))
            {
                options.TokenValidationParameters.NameClaimType = JwtClaimTypes.Subject;
            }
        }
    }
}
