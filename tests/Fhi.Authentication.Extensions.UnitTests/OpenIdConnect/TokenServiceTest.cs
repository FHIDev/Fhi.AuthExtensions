using Duende.AccessTokenManagement;
using Duende.AccessTokenManagement.DPoP;
using Duende.AccessTokenManagement.OpenIdConnect;
using Fhi.Authentication.OpenIdConnect;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Fhi.Authentication.Extensions.UnitTests.OpenIdConnect
{
    public class TokenServiceTest
    {
        [Test]
        public async Task RefreshAccessToken_ReturnsSuccess_WhenNoError()
        {
            var endpoint = Substitute.For<IOpenIdConnectUserTokenEndpoint>();
            endpoint.RefreshAccessTokenAsync(Arg.Any<UserRefreshToken>(), Arg.Any<UserTokenRequestParameters>())
                .Returns(TokenResult.Success(new UserToken { AccessTokenType = AccessTokenType.Parse("Bearer"), Expiration = DateTime.UtcNow.AddHours(1), ClientId = ClientId.Parse("clientId"), AccessToken = AccessToken.Parse("new-at") }));

            var result = await CreateService(endpoint).RefreshAccessTokenAsync("refresh-token");

            Assert.That(result.IsError, Is.False);
        }

        [Test]
        public async Task RefreshAccessToken_ReturnsError_WhenEndpointFails()
        {
            var endpoint = Substitute.For<IOpenIdConnectUserTokenEndpoint>();
            endpoint.RefreshAccessTokenAsync(Arg.Any<UserRefreshToken>(), Arg.Any<UserTokenRequestParameters>())
                .Returns(TokenResult.Failure("invalid_grant", null));

            var result = await CreateService(endpoint).RefreshAccessTokenAsync("refresh-token");

            Assert.That(result.IsError, Is.True);
        }

        [Test]
        public async Task RefreshAccessToken_SendsNullDPoPKey_WhenNoDPoPKeyInOptions()
        {
            var endpoint = Substitute.For<IOpenIdConnectUserTokenEndpoint>();
            endpoint.RefreshAccessTokenAsync(Arg.Any<UserRefreshToken>(), Arg.Any<UserTokenRequestParameters>())
                .Returns(TokenResult.Success(new UserToken { AccessTokenType = AccessTokenType.Parse("Bearer"), Expiration = DateTime.UtcNow.AddHours(1), ClientId = ClientId.Parse("clientId"), AccessToken = AccessToken.Parse("at") }));

            await CreateService(endpoint, CreateOptions(dPoPJsonWebKey: null)).RefreshAccessTokenAsync("refresh-token");

            await endpoint.Received(1).RefreshAccessTokenAsync(
                Arg.Is<UserRefreshToken>(t => t.DPoPProofKey == null),
                Arg.Any<UserTokenRequestParameters>());
        }

        private static IOptions<UserTokenManagementOptions> CreateOptions(string? dPoPJsonWebKey = null)
        {
            var options = Substitute.For<IOptions<UserTokenManagementOptions>>();
            options.Value.Returns(new UserTokenManagementOptions { DPoPJsonWebKey = dPoPJsonWebKey is null ? null : DPoPProofKey.Parse(dPoPJsonWebKey) });
            return options;
        }

        private static ILogger<DefaultTokenService> CreateLogger() =>
            Substitute.For<ILogger<DefaultTokenService>>();

        private static DefaultTokenService CreateService(
            IOpenIdConnectUserTokenEndpoint endpoint,
            IOptions<UserTokenManagementOptions>? options = null,
            ILogger<DefaultTokenService>? logger = null) =>
            new(endpoint, options ?? CreateOptions(), logger ?? CreateLogger());
    }
}
