using System.ComponentModel.DataAnnotations;
using Duende.AccessTokenManagement;
using Fhi.Authentication.ClientCredentials;
using Fhi.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using NSubstitute;

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

        [Test]
        public async Task GIVEN_getClientAssertion_WHEN_expirationSet_THEN_useCorrectExpiration()
        {
            var expirationSeconds = 30;

            var clientCredentialsOptionsMonitor = Substitute.For<IOptionsMonitor<ClientCredentialsClient>>();
            clientCredentialsOptionsMonitor.Get("name").Returns(new ClientCredentialsClient());

            var clientAssertionOptionsMonitor = Substitute.For<IOptionsMonitor<ClientAssertionOptions>>();
            var clientAssertionOptions = new ClientAssertionOptions
            {
                ExpirationSeconds = expirationSeconds,
                Issuer = "issuer",
                PrivateJwk = JWK.Create().PrivateKey
            };

            clientAssertionOptionsMonitor.Get("name").Returns(clientAssertionOptions);

            var clientAssertionService = new ClientCredentialsAssertionService(
                Substitute.For<ILogger<ClientCredentialsAssertionService>>(),
                clientAssertionOptionsMonitor,
                clientCredentialsOptionsMonitor);

            var result = await clientAssertionService.GetClientAssertionAsync(ClientCredentialsClientName.Parse("name"));

            var jwt = new JsonWebToken(result!.Value);
            var expectedExpiration = DateTime.UtcNow.AddSeconds(expirationSeconds);

            // Allow 1 second tolerance for test execution time
            Assert.That(jwt.ValidTo, Is.EqualTo(expectedExpiration).Within(1).Seconds);
        }

        [Test]
        public async Task GIVEN_getClientAssertion_WHEN_expirationIsNotSet_THEN_useDefaultExpiration()
        {
            var clientOptions = Substitute.For<IOptionsMonitor<ClientCredentialsClient>>();
            clientOptions.Get("name").Returns(new ClientCredentialsClient());

            var assertionOptions = Substitute.For<IOptionsMonitor<ClientAssertionOptions>>();
            var options = new ClientAssertionOptions
            {
                Issuer = "issuer",
                PrivateJwk = JWK.Create().PrivateKey
            };

            assertionOptions.Get("name").Returns(options);

            var clientAssertionService = new ClientCredentialsAssertionService(
                Substitute.For<ILogger<ClientCredentialsAssertionService>>(),
                assertionOptions,
                clientOptions);

            var result = await clientAssertionService.GetClientAssertionAsync(ClientCredentialsClientName.Parse("name"));

            var jwt = new JsonWebToken(result!.Value);
            var expectedExpiration = DateTime.UtcNow.AddSeconds(options.ExpirationSeconds);

            // Allow 1 second tolerance for test execution time
            Assert.That(jwt.ValidTo, Is.EqualTo(expectedExpiration).Within(1).Seconds);
        }

        [TestCase(-10)]
        public void GIVEN_clientAssertionOptions_WHEN_expirationSecondsAreNotValid_THEN_dataAnnotationValidatesFalse(int expirationSeconds)
        {
            var options = new ClientAssertionOptions
            {
                Issuer = "issuer",
                PrivateJwk = JWK.Create().PrivateKey,
                ExpirationSeconds = expirationSeconds
            };

            var context = new ValidationContext(options);
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateObject(options, context, results, true);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(isValid, Is.False);
                Assert.That(results, Has.Count.EqualTo(1));
            }

            using (Assert.EnterMultipleScope())
            {
                Assert.That(results[0].ErrorMessage, Does.Contain("ExpirationSeconds must be greater than or equal to 0"));
                Assert.That(results[0].MemberNames, Does.Contain(nameof(ClientAssertionOptions.ExpirationSeconds)));
            }
        }

        [TestCase(0)]
        [TestCase(10)]
        public void GIVEN_clientAssertionOptions_WHEN_expirationSecondsIsValid_THEN_dataAnnotationValidatesTrue(int expirationSeconds)
        {
            var options = new ClientAssertionOptions
            {
                Issuer = "issuer",
                PrivateJwk = JWK.Create().PrivateKey,
                ExpirationSeconds = expirationSeconds
            };

            var context = new ValidationContext(options);
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateObject(options, context, results, true);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(isValid, Is.True);
                Assert.That(results, Is.Empty);
            }
        }

        [Test]
        public async Task GIVEN_getClientAssertion_WHEN_negativeExpirationSeconds_THEN_logErrorAndReturnNull()
        {
            var expirationSeconds = -10; // Invalid value
            var logger = Substitute.For<ILogger<ClientCredentialsAssertionService>>();
            var clientOptions = Substitute.For<IOptionsMonitor<ClientCredentialsClient>>();
            clientOptions.Get("name").Returns(new ClientCredentialsClient());

            var assertionOptions = Substitute.For<IOptionsMonitor<ClientAssertionOptions>>();
            var options = new ClientAssertionOptions
            {
                Issuer = "issuer",
                PrivateJwk = JWK.Create().PrivateKey,
                ExpirationSeconds = expirationSeconds
            };

            assertionOptions.Get("name").Returns(options);

            var clientAssertionService = new ClientCredentialsAssertionService(
                logger,
                assertionOptions,
                clientOptions);
            var result = await clientAssertionService.GetClientAssertionAsync(ClientCredentialsClientName.Parse("name"));

            Assert.That(result, Is.Null);
            logger.Received().Log(
                LogLevel.Error,
                Arg.Any<EventId>(),
                Arg.Is<object>(o => o.ToString()!.Contains("Invalid ExpirationSeconds") && o.ToString()!.Contains($"{expirationSeconds}")),
                Arg.Any<Exception>(),
                Arg.Any<Func<object, Exception?, string>>()
            );
        }
    }
}
