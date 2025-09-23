using Fhi.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;

namespace WebApi.Authorization
{
    /// <summary>
    /// Global handling of the authorization result 
    /// - Enrichs with the WWW-Authenticate header if the user is forbidden.
    /// </summary>
    public class DefaultAccessControlMiddleware : IAuthorizationMiddlewareResultHandler
    {
        private readonly AuthorizationMiddlewareResultHandler defaultHandler = new();

        public async Task HandleAsync(
            RequestDelegate next,
            HttpContext context,
            AuthorizationPolicy policy,
            PolicyAuthorizationResult authorizeResult)
        {
            if (authorizeResult.Forbidden && authorizeResult.AuthorizationFailure is not null)
            {
                var scopeFailure = authorizeResult.AuthorizationFailure.FailureReasons.FirstOrDefault(r => r.Handler is ScopeHandler);
                if (scopeFailure != null)
                {
                    var errorCode = "insufficient_scope";
                    var errorDescription = scopeFailure.Message;
                    await SetWWWAuthenticateHeader(context, policy, errorCode, errorDescription);
                }
            }

            await defaultHandler.HandleAsync(next, context, policy, authorizeResult);
        }

        private static async Task SetWWWAuthenticateHeader(HttpContext context, AuthorizationPolicy policy, string errorCode, string errorDescription)
        {
            var authResult = await context.AuthenticateAsync();
            var scheme = authResult?.Ticket?.AuthenticationScheme
                         ?? policy.AuthenticationSchemes.FirstOrDefault()
                         ?? "";

            context.Response.Headers.Append("WWW-Authenticate",
                $"{scheme} error=\"{errorCode}\", error_description=\"{errorDescription}\"");
        }
    }
}
