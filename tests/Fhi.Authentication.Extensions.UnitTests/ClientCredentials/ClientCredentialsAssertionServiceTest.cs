using System.ComponentModel.DataAnnotations;
using Duende.AccessTokenManagement;
using Fhi.Authentication.ClientCredentials;
using Fhi.Security.Cryptography.Jwks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
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

            var clientAssertionService = new ClientCredentialsAssertionService(
                Substitute.For<ILogger<ClientCredentialsAssertionService>>(),
                assertionOptions,
                clientOptions,
                TimeProvider.System);
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
                Substitute.For<IOptionsMonitor<ClientCredentialsClient>>(),
                TimeProvider.System);

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

            var clientAssertionService = new ClientCredentialsAssertionService(logger, assertionOptions, clientOptions, TimeProvider.System);
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

            var clientAssertionService = new ClientCredentialsAssertionService(logger, assertionOptions, clientOptions, TimeProvider.System);
            var ex = Assert.ThrowsAsync<ArgumentNullException>(async () => await clientAssertionService.GetClientAssertionAsync(ClientCredentialsClientName.Parse("name")));
            Assert.That(ex.Message, Does.Contain("IDX10000: The parameter 'json' cannot be a 'null'"));
        }

        [Test]
        public async Task GIVEN_getClientAssertion_WHEN_expirationSet_THEN_useCorrectExpiration()
        {
            var expirationSeconds = 30;
            var fakeTime = new FakeTimeProvider(new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero));

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
                clientCredentialsOptionsMonitor,
                fakeTime);

            var result = await clientAssertionService.GetClientAssertionAsync(ClientCredentialsClientName.Parse("name"));

            var jwt = new JsonWebToken(result!.Value);
            var expectedExpiration = fakeTime.GetUtcNow().AddSeconds(expirationSeconds).UtcDateTime;

            Assert.That(jwt.ValidTo, Is.EqualTo(expectedExpiration));
        }

        [Test]
        public async Task GIVEN_getClientAssertion_WHEN_expirationIsNotSet_THEN_useDefaultExpiration()
        {
            var fakeTime = new FakeTimeProvider(new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero));

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
                clientOptions,
                fakeTime);

            var result = await clientAssertionService.GetClientAssertionAsync(ClientCredentialsClientName.Parse("name"));

            var jwt = new JsonWebToken(result!.Value);
            var expectedExpiration = fakeTime.GetUtcNow().AddSeconds(options.ExpirationSeconds).UtcDateTime;

            Assert.That(jwt.ValidTo, Is.EqualTo(expectedExpiration));
        }

        [TestCase(-10)]
        [TestCase(0)]
        [TestCase(200)]
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
                Assert.That(results[0].ErrorMessage, Does.Contain("ExpirationSeconds must be between 1 and 120 seconds."));
                Assert.That(results[0].MemberNames, Does.Contain(nameof(ClientAssertionOptions.ExpirationSeconds)));
            }
        }


        [TestCase(1)]
        [TestCase(120)]
        [TestCase(60)]
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
                clientOptions,
                TimeProvider.System);
            var ex = Assert.ThrowsAsync<ArgumentException>(async () => await clientAssertionService.GetClientAssertionAsync(ClientCredentialsClientName.Parse("name")));
            Assert.That(ex.Message, Does.Contain("Expiration"));
        }
    }
}
