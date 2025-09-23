using Fhi.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System.Security.Claims;
using Duende.AccessTokenManagement.OpenIdConnect;

namespace Fhi.Authentication.Extensions.UnitTests.OpenIdConnect
{
    public class CookieEventTests
    {
        [Test]
        public async Task ValidatePrincipal_UserAuthenticatedButNoToken_RejectPrincipalAndRenew()
        {
            var cookieContext = new CookieContextBuilder()
                .WithAuthenticatedPrincipal(true)
                .Build();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(cookieContext.ShouldRenew, Is.False, "Authenticated user");
                Assert.That(cookieContext.Principal!.Identity!.IsAuthenticated, Is.True, "Authenticated user");
            }

            var cookieEvent = new OpenIdConnectCookieEventsForApi(Substitute.For<ILogger<OpenIdConnectCookieEventsForApi>>());
            await cookieEvent.ValidatePrincipal(cookieContext);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(cookieContext.ShouldRenew, Is.True, "Expected RejectPrincipalAndRenew result");
                Assert.That(cookieContext.Principal, Is.Null, "Not authenticated user");
            }
        }

        [Test]
        public async Task ValidatePrincipal_UserAuthenticatedTokenAndAccessTokenExpiredButRefreshTokenValid_ShouldNotRenew()
        {
            var cookieContext = new CookieContextBuilder()
                .WithAuthenticatedPrincipal(true)
                .WithRefreshAccessTokenError(false)
                .WithTokens(
                    new AuthenticationToken { Name = "access_token", Value = "access_token_value" },
                    new AuthenticationToken { Name = "refresh_token", Value = "refresh_token_value" },
                    new AuthenticationToken { Name = "expires_at", Value = DateTimeOffset.UtcNow.AddMinutes(-1).ToString("o") }
                )
                .Build();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(cookieContext.ShouldRenew, Is.False, "Authenticated user");
                Assert.That(cookieContext.Principal!.Identity!.IsAuthenticated, Is.True, "Authenticated user");
            }

            var cookieEvent = new OpenIdConnectCookieEventsForApi(Substitute.For<ILogger<OpenIdConnectCookieEventsForApi>>());
            await cookieEvent.ValidatePrincipal(cookieContext);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(cookieContext.ShouldRenew, Is.False, "Authenticated user");
                Assert.That(cookieContext.Principal.Identity.IsAuthenticated, Is.True, "Authenticated user");
            }
        }

        [Test]
        public async Task ValidatePrincipal_UserAuthenticatedAndTokensNotExpired_ShouldNotRenew()
        {
            var cookieContext = new CookieContextBuilder()
                .WithRefreshAccessTokenError(false)
                .WithTokens(
                    new AuthenticationToken { Name = "access_token", Value = "access_token_value" },
                    new AuthenticationToken { Name = "refresh_token", Value = "refresh_token_value" },
                    new AuthenticationToken { Name = "expires_at", Value = DateTimeOffset.UtcNow.AddMinutes(10).ToString("o") }
                )
                .Build();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(cookieContext.ShouldRenew, Is.False, "Authenticated user");
                Assert.That(cookieContext.Principal!.Identity!.IsAuthenticated, Is.True, "Authenticated user");
            }

            var cookieEvent = new OpenIdConnectCookieEventsForApi(Substitute.For<ILogger<OpenIdConnectCookieEventsForApi>>());
            await cookieEvent.ValidatePrincipal(cookieContext);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(cookieContext.ShouldRenew, Is.False, "Should not renew when tokens are valid");
                Assert.That(cookieContext.Principal.Identity.IsAuthenticated, Is.True, "Authenticated user");
            }
        }

        [Test]
        public async Task ValidatePrincipal_MissingRefreshToken_RejectPrincipalAndRenew()
        {
            var cookieContext = new CookieContextBuilder()
                .WithAuthenticatedPrincipal(true)
                .WithRefreshAccessTokenError(false)
                .WithTokens(
                    new AuthenticationToken { Name = "access_token", Value = "access_token_value" },
                    new AuthenticationToken { Name = "expires_at", Value = DateTimeOffset.UtcNow.AddMinutes(-1).ToString("o") }
                )
                .Build();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(cookieContext.ShouldRenew, Is.False, "Authenticated user");
                Assert.That(cookieContext.Principal!.Identity!.IsAuthenticated, Is.True, "Authenticated user");
            }

            var cookieEvent = new OpenIdConnectCookieEventsForApi(Substitute.For<ILogger<OpenIdConnectCookieEventsForApi>>());
            await cookieEvent.ValidatePrincipal(cookieContext);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(cookieContext.ShouldRenew, Is.True, "Should renew when refresh token is missing");
                Assert.That(cookieContext.Principal, Is.Null, "Not authenticated user");
            }
        }
    }

    internal class CookieContextBuilder
    {
        private readonly List<AuthenticationToken> _tokens = [];
        private bool _isAuthenticated = true;
        private readonly IUserTokenEndpointService _userTokenEndpointService = Substitute.For<IUserTokenEndpointService>();

        public CookieContextBuilder WithRefreshAccessTokenError(bool isError)
        {
            var userToken = isError ? new UserToken { Error = "invalid_grant" } : new UserToken();
            _userTokenEndpointService
                .RefreshAccessTokenAsync(Arg.Any<UserToken>(), Arg.Any<UserTokenRequestParameters>())
                .Returns(Task.FromResult(userToken));
            return this;
        }

        public CookieContextBuilder WithTokens(params AuthenticationToken[] tokens)
        {
            _tokens.Clear();
            _tokens.AddRange(tokens);
            return this;
        }

        public CookieContextBuilder WithAuthenticatedPrincipal(bool isAuthenticated)
        {
            _isAuthenticated = isAuthenticated;
            return this;
        }

        public CookieValidatePrincipalContext Build()
        {
            var identity = new ClaimsIdentity([new("sub", "sub value")], "Cookies");
            if (!_isAuthenticated)
                identity = new ClaimsIdentity();
            var principal = new ClaimsPrincipal(identity);
            var props = new AuthenticationProperties();
            props.StoreTokens(_tokens);
            var services = new ServiceCollection();
            services.AddSingleton(_userTokenEndpointService);
            var serviceProvider = services.BuildServiceProvider();
            var httpContext = new DefaultHttpContext { RequestServices = serviceProvider };
            return new CookieValidatePrincipalContext(
                httpContext,
                new AuthenticationScheme("oidc", "oidc", typeof(CookieAuthenticationHandler)),
                new CookieAuthenticationOptions(),
                new AuthenticationTicket(principal, props, "Cookies")
            );
        }
    }
}
