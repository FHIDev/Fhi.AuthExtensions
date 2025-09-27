using Duende.AccessTokenManagement.OpenIdConnect;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;

namespace BlazorInteractiveServer.Hosting.Authentication
{
    public class CustomRevalidatingAuthenticationStateProvider
    : RevalidatingServerAuthenticationStateProvider
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public CustomRevalidatingAuthenticationStateProvider(
            ILoggerFactory loggerFactory,
            IServiceScopeFactory scopeFactory)
            : base(loggerFactory)
        {
            _scopeFactory = scopeFactory;
        }

        protected override async Task<bool> ValidateAuthenticationStateAsync(AuthenticationState authenticationState, CancellationToken cancellationToken)
        {
            var user = authenticationState.User;

            if (!user.Identity?.IsAuthenticated ?? true)
                return false;

            using var scope = _scopeFactory.CreateScope();
            var userTokenStore = scope.ServiceProvider.GetRequiredService<IUserTokenStore>();
            var token = await userTokenStore.GetTokenAsync(authenticationState.User);
            if (token is null || token.Token?.TokenForSpecifiedParameters?.Expiration < DateTimeOffset.UtcNow || !token.Succeeded)
            {
                return false;
            }
            return true;
        }

        protected override TimeSpan RevalidationInterval => TimeSpan.FromMinutes(1);
    }
}
