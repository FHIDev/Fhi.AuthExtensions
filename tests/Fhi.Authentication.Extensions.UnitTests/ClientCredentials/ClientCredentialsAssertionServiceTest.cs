using Duende.AccessTokenManagement;
using Fhi.Authentication.ClientCredentials;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using NSubstitute;
using Fhi.Security.Cryptography;

namespace Fhi.Authentication.Extensions.UnitTests.ClientCredentials
{
    public class ClientCredentialsAssertionServiceTest
    {
        [Test]
        public async Task GIVEN_getClientAssertion_WHEN_clientExist_THEN_returnAssertion()
        {
            var jwk = JWK.Create();
            var clientOptions = Substitute.For<IOptionsMonitor<ClientCredentialsClient>>();
            clientOptions.Get("name").Returns(new ClientCredentialsClient
            {
                ClientId = ClientId.Parse("client-id"),
                Scope = null
            });
            var assertionOptions = Substitute.For<IOptionsMonitor<ClientAssertionOptions>>();
            assertionOptions.Get("name").Returns(new ClientAssertionOptions
            {
                Issuer = "issuer",
                PrivateJwk = jwk.PrivateKey,
                ClientAssertionType = "urn:ietf:params:oauth:client-assertion-type:jwt-bearer"
            });

            var clientAssertionService = new ClientCredentialsAssertionService(Substitute.For<ILogger<ClientCredentialsAssertionService>>(), assertionOptions, clientOptions);
            var result = await clientAssertionService.GetClientAssertionAsync(ClientCredentialsClientName.Parse("name"));

            var jwt = new JsonWebToken(result!.Value);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(jwt!.Issuer, Is.EqualTo("client-id"));
                Assert.That(jwt.Audiences, Does.Contain("issuer"));
            }
        }

        [Test]
        public async Task GIVEN_getClientAssertion_WHEN_clientNotExist_THEN_logError()
        {
            var logger = Substitute.For<ILogger<ClientCredentialsAssertionService>>();
            var assertionOptions = Substitute.For<IOptionsMonitor<ClientAssertionOptions>>();
            var clientAssertionService = new ClientCredentialsAssertionService(
                logger,
                assertionOptions,
                Substitute.For<IOptionsMonitor<ClientCredentialsClient>>());

            var result = await clientAssertionService.GetClientAssertionAsync(ClientCredentialsClientName.Parse("non-existing-client"));

            logger.Received().Log(
                LogLevel.Error,
                Arg.Any<EventId>(),
                Arg.Is<object>(o => o.ToString()!.Contains("Could not resolve options for client non-existing-client")),
                Arg.Any<Exception>(),
                Arg.Any<Func<object, Exception?, string>>()
            );
        }

        [TestCase("")]
        [TestCase(null)]
        public async Task GIVEN_getClientAssertion_WHEN_issuerParamteterNullOrEmpty_THEN_logError(string? issuer)
        {
            var logger = Substitute.For<ILogger<ClientCredentialsAssertionService>>();
            var clientOptions = Substitute.For<IOptionsMonitor<ClientCredentialsClient>>();
            clientOptions.Get("name").Returns(new ClientCredentialsClient
            {
                ClientId = ClientId.Parse("client-id"),
                Scope = null
            });
            var assertionOptions = Substitute.For<IOptionsMonitor<ClientAssertionOptions>>();
            assertionOptions.Get("name").Returns(new ClientAssertionOptions
            {
                Issuer = issuer!,
                PrivateJwk = "jwk"
            });

            var clientAssertionService = new ClientCredentialsAssertionService(logger, assertionOptions, clientOptions);
            var result = await clientAssertionService.GetClientAssertionAsync(ClientCredentialsClientName.Parse("name"));

            logger.Received().Log(
             LogLevel.Error,
             Arg.Any<EventId>(),
             Arg.Is<object>(o => o.ToString()!.Contains("Could not resolve issuer for name. Missing parameter")),
             Arg.Any<Exception>(),
             Arg.Any<Func<object, Exception?, string>>()
         );
        }

        [TestCase("")]
        [TestCase(null)]
        public async Task GIVEN_getClientAssertion_WHEN_jwkParamteterNullOrEmpty_THEN_logError(string? jwk)
        {
            var logger = Substitute.For<ILogger<ClientCredentialsAssertionService>>();
            var clientOptions = Substitute.For<IOptionsMonitor<ClientCredentialsClient>>();
            clientOptions.Get("name").Returns(new ClientCredentialsClient
            {
                ClientId = ClientId.Parse("client-id"),
                Scope = null
            });
            var assertionOptions = Substitute.For<IOptionsMonitor<ClientAssertionOptions>>();
            assertionOptions.Get("name").Returns(new ClientAssertionOptions
            {
                Issuer = "issuer",
                PrivateJwk = jwk!
            });

            var clientAssertionService = new ClientCredentialsAssertionService(logger, assertionOptions, clientOptions);
            var result = await clientAssertionService.GetClientAssertionAsync(ClientCredentialsClientName.Parse("name"));

            logger.Received().Log(
             LogLevel.Error,
             Arg.Any<EventId>(),
             Arg.Is<object>(o => o.ToString()!.Contains("Could not resolve JWK")),
             Arg.Any<Exception>(),
             Arg.Any<Func<object, Exception?, string>>()
         );
        }
    }
}
