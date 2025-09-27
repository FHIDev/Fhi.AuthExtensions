using Duende.AccessTokenManagement;
using Duende.AccessTokenManagement.OpenIdConnect;
using Microsoft.Extensions.Logging;

namespace Fhi.Authentication.OpenIdConnect
{
    /// <summary>
    /// Response for token validation.
    /// </summary>
    /// <param name="IsError"></param>
    public record TokenResponse(bool IsError = false);
    /// <summary>
    /// Abstraction for token service.
    /// TODO: response should be improved
    /// </summary>
    public interface ITokenService
    {
        /// <summary>
        /// Refresh access token.
        /// </summary>
        /// <param name="refreshToken"></param>
        /// <returns></returns>
        Task<TokenResponse> RefreshAccessTokenAsync(string refreshToken);
    }

    internal class DefaultTokenService(IOpenIdConnectUserTokenEndpoint UserTokenEndpointService, ILogger<DefaultTokenService> Logger) : ITokenService
    {
        public async Task<TokenResponse> RefreshAccessTokenAsync(string refreshToken)
        {
            var userToken = await UserTokenEndpointService.RefreshAccessTokenAsync(new UserRefreshToken(RefreshToken.Parse(refreshToken), null), new UserTokenRequestParameters());
            
            if (userToken.FailedResult is not null)
            {
                Logger.LogError(message: userToken.FailedResult.Error);
                return new TokenResponse(true);
            }

            return new TokenResponse(false);
        }
    }
}
