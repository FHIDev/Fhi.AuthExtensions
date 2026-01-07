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

        [TestCase(null, Description = "Default expiration (10 seconds)")]
        [TestCase(30, Description = "Custom expiration (30 seconds)")]
        [TestCase(120, Description = "Custom expiration (120 seconds)")]
        public async Task GIVEN_getClientAssertion_WHEN_expirationSet_THEN_useCorrectExpiration(int? expirationSeconds)
        {
            var jwk = JWK.Create();
            var clientOptions = Substitute.For<IOptionsMonitor<ClientCredentialsClient>>();
            clientOptions.Get("name").Returns(new ClientCredentialsClient
            {
                ClientId = ClientId.Parse("client-id"),
                Scope = null
            });
            
            var assertionOptions = Substitute.For<IOptionsMonitor<ClientAssertionOptions>>();
            var options = new ClientAssertionOptions
            {
                Issuer = "issuer",
                PrivateJwk = jwk.PrivateKey,
                ClientAssertionType = "urn:ietf:params:oauth:client-assertion-type:jwt-bearer"
            };
            
            // Only set ExpirationSeconds if a value is provided (null means use default)
            if (expirationSeconds.HasValue)
            {
                options.ExpirationSeconds = expirationSeconds.Value;
            }
            
            assertionOptions.Get("name").Returns(options);

            var clientAssertionService = new ClientCredentialsAssertionService(
                Substitute.For<ILogger<ClientCredentialsAssertionService>>(), 
                assertionOptions, 
                clientOptions);
            var result = await clientAssertionService.GetClientAssertionAsync(ClientCredentialsClientName.Parse("name"));

            var jwt = new JsonWebToken(result!.Value);
            var expirationTime = jwt.ValidTo;
            var expectedSeconds = expirationSeconds ?? 10; // Use default of 10 if null
            var expectedExpiration = DateTime.UtcNow.AddSeconds(expectedSeconds);
            
            // Allow 2 second tolerance for test execution time
            Assert.That(Math.Abs((expirationTime - expectedExpiration).TotalSeconds), Is.LessThanOrEqualTo(2),
                $"Expected expiration around {expectedSeconds} seconds from now, but got {expirationTime}");
        }

        [TestCase(-10, false)]
        [TestCase(0, true)]
        [TestCase(10, true)]
        public void GIVEN_clientAssertionOptions_WHEN_expirationSecondsValidated_THEN_dataAnnotationValidatesCorrectly(int expirationSeconds, bool expectedValid)
        {
            var options = new ClientAssertionOptions
            {
                Issuer = "issuer",
                PrivateJwk = "jwk",
                ExpirationSeconds = expirationSeconds
            };

            var context = new System.ComponentModel.DataAnnotations.ValidationContext(options);
            var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
            var isValid = System.ComponentModel.DataAnnotations.Validator.TryValidateObject(options, context, results, true);

            Assert.That(isValid, Is.EqualTo(expectedValid), 
                $"Validation should {(expectedValid ? "succeed" : "fail")} for ExpirationSeconds = {expirationSeconds}");
            
            if (!expectedValid)
            {
                Assert.That(results, Has.Count.EqualTo(1));
                Assert.That(results[0].ErrorMessage, Does.Contain("ExpirationSeconds must be greater than or equal to 0"));
                Assert.That(results[0].MemberNames, Does.Contain(nameof(ClientAssertionOptions.ExpirationSeconds)));
            }
            else
            {
                Assert.That(results, Is.Empty);
            }
        }

        [Test]
        public async Task GIVEN_getClientAssertion_WHEN_negativeExpirationSeconds_THEN_logErrorAndReturnNull()
        {
            var jwk = JWK.Create();
            var logger = Substitute.For<ILogger<ClientCredentialsAssertionService>>();
            var clientOptions = Substitute.For<IOptionsMonitor<ClientCredentialsClient>>();
            clientOptions.Get("name").Returns(new ClientCredentialsClient
            {
                ClientId = ClientId.Parse("client-id"),
                Scope = null
            });
            
            var assertionOptions = Substitute.For<IOptionsMonitor<ClientAssertionOptions>>();
            var options = new ClientAssertionOptions
            {
                Issuer = "issuer",
                PrivateJwk = jwk.PrivateKey,
                ClientAssertionType = "urn:ietf:params:oauth:client-assertion-type:jwt-bearer",
                ExpirationSeconds = -10 // Invalid value
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
                Arg.Is<object>(o => o.ToString()!.Contains("Invalid ExpirationSeconds") && o.ToString()!.Contains("-10")),
                Arg.Any<Exception>(),
                Arg.Any<Func<object, Exception?, string>>()
            );
        }
    }
}
