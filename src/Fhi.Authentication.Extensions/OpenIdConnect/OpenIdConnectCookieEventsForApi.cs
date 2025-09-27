using Duende.AccessTokenManagement.OpenIdConnect;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Logging;

namespace Fhi.Authentication.OpenIdConnect
{
    /// <summary>
    /// Default implementation of <see cref="CookieAuthenticationEvents"/> for web clients using OpenId Connect with downstream API calls
    /// with refresh token.
    /// </summary>
    public class OpenIdConnectCookieEventsForApi(ILogger<OpenIdConnectCookieEventsForApi> logger) : BasicCookieAuthenticationEvents
    {
        private readonly ILogger<OpenIdConnectCookieEventsForApi> _logger = logger;

        /// <summary>
        /// Validate the principal and check if the access token or refresh token is expired.
        /// This is not needed if you do not have an downstream API that requires access tokens.
        /// </summary>
        /// <param name="context">CookieValidatePrincipalContext</param>
        /// <returns></returns>
        public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
        {
            var validationResult = await context.ValidateToken();
            if (validationResult.IsError)
            {
                _logger.LogError("Token validation error: {Error} - {ErrorDescription}", validationResult.Error, validationResult.ErrorDescription);
                context.RejectPrincipal();
                context.ShouldRenew = true;
            }
        }

        /// <summary>
        /// This method is called when the user is signing out. Revokes refresh tokens.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public override async Task SigningOut(CookieSigningOutContext context)
        {
            await context.HttpContext.RevokeRefreshTokenAsync();

            await base.SigningOut(context);
        }

    }

    /// <summary>
    /// Basic handling of cookie authentication events.
    /// </summary>
    public class BasicCookieAuthenticationEvents : CookieAuthenticationEvents
    {

    }
}
