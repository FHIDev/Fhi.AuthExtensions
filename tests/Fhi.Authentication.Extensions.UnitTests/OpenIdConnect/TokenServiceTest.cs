using Duende.AccessTokenManagement.OpenIdConnect;
using Fhi.Authentication.OpenIdConnect;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Fhi.Authentication.Extensions.UnitTests.OpenIdConnect
{
    //public class TokenServiceTest
    //{
    //    [Test]
    //    public async Task RefreshAccessToken_ReturnsSuccess_WhenNoError()
    //    {
    //        var userTokenEndpointService = Substitute.For<IUserTokenEndpointService>();
    //        var logger = Substitute.For<ILogger<DefaultTokenService>>();
    //        var refreshToken = "refresh-token";
    //        userTokenEndpointService.RefreshAccessTokenAsync(
    //            Arg.Any<UserToken>(), Arg.Any<UserTokenRequestParameters>())
    //            .Returns(Task.FromResult(new UserToken()));

    //        var service = new DefaultTokenService(userTokenEndpointService, logger);
    //        var result = await service.RefreshAccessTokenAsync(refreshToken);

    //        Assert.That(result.IsError, Is.False);
    //    }

    //    [Test]
    //    public async Task RefreshAccessToken_ReturnsError_WhenUserTokenIsError()
    //    {
    //        var userTokenEndpointService = Substitute.For<IUserTokenEndpointService>();
    //        var logger = Substitute.For<ILogger<DefaultTokenService>>();
    //        var refreshToken = "refresh-token";
    //        userTokenEndpointService.RefreshAccessTokenAsync(
    //            Arg.Any<UserToken>(), Arg.Any<UserTokenRequestParameters>())
    //            .Returns(Task.FromResult(new UserToken { Error = "invalid grant" }));

    //        var service = new DefaultTokenService(userTokenEndpointService, logger);
    //        var result = await service.RefreshAccessTokenAsync(refreshToken);

    //        Assert.That(result.IsError, Is.True);
    //    }
    //}
}
