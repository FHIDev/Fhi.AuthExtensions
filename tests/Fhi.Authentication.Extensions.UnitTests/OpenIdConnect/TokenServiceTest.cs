using Duende.AccessTokenManagement;
using Duende.AccessTokenManagement.DPoP;
using Duende.AccessTokenManagement.OpenIdConnect;
using Fhi.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System.Security.Claims;

namespace Fhi.Authentication.Extensions.UnitTests.OpenIdConnect
{
    public class TokenServiceTest
    {
        private readonly IOpenIdConnectUserTokenEndpoint _endpoint;
        private readonly IUserTokenStore _store;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<DefaultTokenService> _logger;
        private readonly DefaultTokenService _service;

        public TokenServiceTest()
        {
            _endpoint = Substitute.For<IOpenIdConnectUserTokenEndpoint>();
            _store = Substitute.For<IUserTokenStore>();
            _httpContextAccessor = Substitute.For<IHttpContextAccessor>();
            _logger = Substitute.For<ILogger<DefaultTokenService>>();

            _service = new DefaultTokenService(_endpoint, _store, _httpContextAccessor, _logger);
        }

        [Test]
        public async Task RefreshAccessToken_ReturnsSuccess_WhenNoError()
        {
            var principal = new ClaimsPrincipal(new ClaimsIdentity("test"));
            _httpContextAccessor.HttpContext.Returns(new DefaultHttpContext { User = principal });
            _store.GetTokenAsync(principal).Returns(TokenResult.Success(new TokenForParameters(new UserToken { AccessToken = AccessToken.Parse("at") })));
            _endpoint.RefreshAccessTokenAsync(Arg.Any<UserRefreshToken>(), Arg.Any<UserTokenRequestParameters>())
                .Returns(TokenResult.Success(new UserToken { AccessToken = AccessToken.Parse("new-at") }));

            var result = await _service.RefreshAccessTokenAsync("refresh-token");

            Assert.That(result.IsError, Is.False);
        }

        [Test]
        public async Task RefreshAccessToken_ReturnsError_WhenEndpointFails()
        {
            var principal = new ClaimsPrincipal(new ClaimsIdentity("test"));
            _httpContextAccessor.HttpContext.Returns(new DefaultHttpContext { User = principal });
            _store.GetTokenAsync(principal).Returns(TokenResult.Success(new TokenForParameters(new UserToken { AccessToken = AccessToken.Parse("at") })));
            _endpoint.RefreshAccessTokenAsync(Arg.Any<UserRefreshToken>(), Arg.Any<UserTokenRequestParameters>())
                .Returns(TokenResult.Failure("invalid_grant", null));

            var result = await _service.RefreshAccessTokenAsync("refresh-token");

            Assert.That(result.IsError, Is.True);
        }

        [Test]
        public async Task RefreshAccessToken_SendsNullDPoPKey_WhenNoUserInContext()
        {
            _httpContextAccessor.HttpContext.Returns((HttpContext?)null);
            _endpoint.RefreshAccessTokenAsync(Arg.Any<UserRefreshToken>(), Arg.Any<UserTokenRequestParameters>())
                .Returns(TokenResult.Success(new UserToken { AccessToken = AccessToken.Parse("at") }));

            await _service.RefreshAccessTokenAsync("refresh-token");

            await _endpoint.Received(1).RefreshAccessTokenAsync(
                Arg.Is<UserRefreshToken>(t => t.DPoPProofKey == null),
                Arg.Any<UserTokenRequestParameters>());
        }
    }
}
