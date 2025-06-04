using Duende.AccessTokenManagement.OpenIdConnect;
using Duende.IdentityModel;
using System.Collections.Concurrent;
using System.Security.Claims;

/// <summary>
/// Blazor Server keeps a persistent SignalR connection. That connection does not have access to HttpContext or cookies directly.
/// See: https://learn.microsoft.com/en-us/aspnet/core/blazor/state-management?view=aspnetcore-9.0
/// This stores tokens in memory. For applications that use multiple instances or do not use sticky sessions, user state should be stored in persistent storage
/// See: https://learn.microsoft.com/en-us/aspnet/core/blazor/state-management?view=aspnetcore-9.0#server-side-storage-server
/// </summary>
public class InMemoryUserTokenStore : IUserTokenStore
{
    private static readonly ConcurrentDictionary<string, UserToken> _tokenStore = new();

    public Task StoreTokenAsync(ClaimsPrincipal user, UserToken token, UserTokenRequestParameters? parameters = null)
    {
        var userId = user.FindFirst(JwtClaimTypes.Subject)?.Value;
        if (userId != null)
        {
            _tokenStore[userId] = token;
        }
        return Task.CompletedTask;
    }

    public Task<UserToken> GetTokenAsync(ClaimsPrincipal user, UserTokenRequestParameters? parameters = null)
    {
        var userId = user.FindFirst(JwtClaimTypes.Subject)?.Value;
        if (userId != null && _tokenStore.TryGetValue(userId, out var token))
        {
            return Task.FromResult<UserToken>(token);
        }
        return Task.FromResult<UserToken>(new UserToken());
    }

    public Task ClearTokenAsync(ClaimsPrincipal user, UserTokenRequestParameters? parameters = null)
    {
        var userId = user.FindFirst(JwtClaimTypes.Subject)?.Value;
        if (userId != null)
        {
            _tokenStore.Remove(userId, out _);
        }
        return Task.CompletedTask;
    }
}