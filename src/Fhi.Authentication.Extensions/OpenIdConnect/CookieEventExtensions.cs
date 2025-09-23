using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using System.Globalization;
using Duende.AccessTokenManagement.OpenIdConnect;

namespace Fhi.Authentication.OpenIdConnect
{
    /// <summary>
    /// Extensions for handling cookie events in OpenID Connect authentication.
    /// </summary>
    public static partial class CookieEventExtensions
    {
        private const string ExpiresAt = "expires_at";

        /// <summary>
        /// This override is required when using downstream APIs that rely on access tokens.
        /// 
        /// The authentication cookie may remain valid even after the access and refresh tokens have expired.
        /// In such cases, the user will still appear as logged in (based on the cookie), but calls to downstream APIs 
        /// will fail with a 401 Unauthorized error because the tokens are no longer valid.
        /// 
        /// This method ensures that the refresh token is still valid. If it is expired, the principal is rejected,
        /// prompting the authentication system to renew the cookie (and potentially trigger a re-login).
        /// 
        /// Note: This mechanism must be used in combination with a configured <c>ExpireTimeSpan</c> for the cookie,
        /// to align cookie lifetime management with token lifetimes.
        /// </summary>
        /// <param name="context">The context for validating the authentication principal.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public static async Task<TokenValidationResponse> ValidateToken(this CookieValidatePrincipalContext context)
        {
            if (context.Principal?.Identity is not null && context.Principal.Identity.IsAuthenticated)
            {
                var tokens = context.Properties.GetTokens();
                var accessToken = tokens.SingleOrDefault(t => t.Name == OpenIdConnectParameterNames.AccessToken);
                var refreshToken = tokens.SingleOrDefault(t => t.Name == OpenIdConnectParameterNames.RefreshToken);

                if (accessToken == null || string.IsNullOrEmpty(accessToken.Value) || refreshToken == null || string.IsNullOrEmpty(refreshToken.Value))
                {
                    return new TokenValidationResponse(true, TokenValidationErrorCodes.NotFound, "Access token or refresh token is missing. Rejecting principal and renewing cookie.");
                }

                var expiresAt = DateTimeOffset.Parse(tokens.SingleOrDefault(t => t.Name == ExpiresAt)?.Value ?? string.Empty, CultureInfo.InvariantCulture);
                if (expiresAt <= DateTimeOffset.UtcNow)
                {
                    var userTokenEndpointService  = context.HttpContext.RequestServices.GetRequiredService<IUserTokenEndpointService>();
                    var refreshedTokens = await userTokenEndpointService.RefreshAccessTokenAsync(new UserToken() { RefreshToken = refreshToken.Value }, new UserTokenRequestParameters());
                    if (refreshedTokens.IsError)
                    {
                        return new TokenValidationResponse(true, TokenValidationErrorCodes.ExpiredRefreshToken, "Refresh token is expired. Rejecting principal so that the user can re-authenticate");
                    }
                }
            }

            return new TokenValidationResponse(false);
        }
    }
}
