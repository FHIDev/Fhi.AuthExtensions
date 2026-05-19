using System.ComponentModel.DataAnnotations;
using Duende.AccessTokenManagement;
using Fhi.Authentication.ClientCredentials;
using Fhi.Authentication.OpenIdConnect;
using Fhi.Security.Cryptography.Jwks;
using Microsoft.Extensions.DependencyInjection;
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

        [Test]
        public async Task GIVEN_getClientAssertion_WHEN_calledWithUnregisteredClientName_THEN_returnNullWithoutLoggingError()
        {
            // Duende's OpenIdConnectUserTokenEndpoint calls GetClientAssertionAsync
            // with an OIDC scheme-derived client name (e.g.
            // "Duende.TokenManagement.SchemeBasedClient:HelseId") during token refresh.
            // The service should return null for unregistered clients so Duende
            // falls back to the OIDC scheme's own client credentials.
            // It should NOT log an error — this is normal behavior, not a failure.
            var registeredClientName = "MyM2MClient";
            var unregisteredClientName = "Duende.TokenManagement.SchemeBasedClient:HelseId";
            var discoveryStore = Substitute.For<IDiscoveryDocumentStore>();
            discoveryStore.Get("https://authority").Returns(
                new DiscoveryDocument("https://authority", "https://issuer", null, "https://authority/connect/token", null, null, null));

            var loggerMock = Substitute.For<ILogger<ClientCredentialsAssertionService>>();
            var provider = new ServiceCollection()
                .AddSingleton(discoveryStore)
                .AddSingleton(loggerMock)
                .AddClientCredentialsClientOptions(
                    registeredClientName,
                    "https://authority",
                    "my-client-id",
                    PrivateJwk.ParseFromBase64Encoded(TestJwkBase64),
                    "scope1")
                .Services
                .BuildServiceProvider();

            var assertionService = provider.GetRequiredService<IClientAssertionService>();

            var result = await assertionService.GetClientAssertionAsync(
                ClientCredentialsClientName.Parse(unregisteredClientName));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.Null);
                loggerMock.DidNotReceive().Log(
                    LogLevel.Error,
                    Arg.Any<EventId>(),
                    Arg.Any<object>(),
                    Arg.Any<Exception>(),
                    Arg.Any<Func<object, Exception?, string>>());
            }
        }

        // Minimal RSA JWK for testing (same key used in ClientCredentialsExtensionsTests)
        private const string TestJwkBase64 = "ewogICJhbGciOiAiUlM1MTIiLAogICJkIjogIjJoRnZSYXdqODBjdFpwZU96R0NsU19ObW1nSTFwbmVJS1BEbktJb2lzT2hXWmRCTTc1SGhGUi1mMktkVjQtdVpKRktHZThsYl9vbWVpMDhrbkppeTRXTnlKdnNhRzRXQUFoZC1EZEpUYUh5OFJEUG9pUV9zaDhrVzc5Y2hTcHhFUlVCc2xKTUhqa1JDNzFMeVdua1hnd1BBeFpVdUxINGdpd2MyRmVlTUNrbGpWREFvSngydkxsTE1ueDh5cm5NZWNmXzNzYkE0bjNkQW9Fb0ZBcXB2QWdEZlRmUHJVUXJ6TGdLWXNYMy1TTTdnb2VLUWRtdUZKLTZKZUZBSmRDdFpNUmJqT1l6OWRZS3p6cWFmMkxrcUs5clZJVGlzTWNvSEwzRnFFbmprTlBmZ1Q5RTc3b0ZhU2puckNia1dRUjZ6ZmhRQWpaY0Z6RXZHTU1ydTZNWG1tcllHbTNmRnlGTEtiODRaOGcwVU9aT0FYaEl5UTBBS2RWTzZSVnFCR2hwaUl2VDJGbEQzaXZyOGw1MmZMZGVvZHdpUThvTW1zalUzYmtGMTVyMWZWZ2NtYXk1WTAtV2NVdHRaN2tfbXBSVTVLcUtjZVhmR1RGSVA3Y0E0cEdmQkZ4bk9EQ0s2ZDFnVTdGNEJhZ2FHNU1XaEtfTnZ3MjV5aU8zdGt1cXM4NDAwenF3Z1Z1VlFQWlFnYmZLREtIb1NKOU01STc3d0d0NEk0Ukx2bUo1bFFwcVM2eFdKVWRvalRnOHF2UVFTVkMtVlo5bml2ekt5bzhhWTBTM3I0RjhXUlNjUXNNTl9EazkwX1JHMnlRdzcwdHgzSkdfRHhYc3BlQmdYR3RvWHBsTlpLekx0WWVFVG01WF95NE5rZ3pzcEJqWnhsNWNvVDFmSGtuVF9WUW5BQmVFIiwKICAiZHAiOiAiSkJNX2Y4QjlpOTZmYWd2M2ZHdUhEQ1RnUHo2eWJkLWNVRzh2LXBEQ1FCRllKdGZiUDNnSUZUS3RtaVo1MkFkUmJDVW1JNnc3QmJIaC1ONk9Vd3FCbktIb1hnMHhjRkY3NUN2OVVQNUdWNVJjVXVKaDFyNDdxOWF5Uy1rZWpZWFJBcXk0akgxbXFvN2h3Ti1TSGxPT0RvdlFYdHFET3REQV9reVUxblpSN3dVamdvWDEtMUlBZlZVM0VGT3A2S3hmM3NYQWdaLTZ3YTZTbU5jbWdITkZlSEs1QTI0R3dWRVVRSWZMSVVXTGNIbDNUZFFGUEJoZEhXZjZVN2Rvd0NhT2xIY3NQZ3BNQmlwZE5JeV8tTk1Jb0taM1dKVVNUN1NQRG1hdFVtX2pteXhrNkhCd0hwQzhJNTZmRW5yeTB0R3NOWHJjcWthTFNRR2pYYklTTWJuczRRIiwKICAiZHEiOiAieFdRTjFGYUVUaVcwQ0ZqaVVQU0taM2RvVEFVRmVDZlJtNFduUFFXdmRmOU96ZzRaLXByZkVGZWJocGN6MW5HVTFqNTFsdVNneEVtLU9yN2FWcC14aGEtNV9Wem9TNTVteUhmR3RjNjNqU244NjZqbGpwUlYtRVFBdGlEcUgwWEQxLUREWmE0R3pNZDFKTTFLNTJPTHMyaHVQd045NXJLSExnUTB5Yk10b1I5VkNkWUpoWFowV2dQNC1nc0dlaEtwUE9PVnVjTlVMdGJmVFZQSkQ0NlNObDlGb3VaTHlFWVhHZzRLNkRpYWt5bUpTT1dCVlFvbDFfT3E2OTFBN1FLODExY2MzSnlkODJhYnZCaHk5SThxTHBpSVRQWWZ0Sko1Q244U3NtZ2F0Zjk4ay1XOVdGeFlRMFFJZ1E0X3FiYjMxZDM3WjZGMkhneHJ0NTBoNm9QR0JRIiwKICAiZSI6ICJBUUFCIiwKICAia2V5X29wcyI6IFtdLAogICJraWQiOiAiYXV0aGV4dGVuc2lvbi5zYW1wbGUiLAogICJrdHkiOiAiUlNBIiwKICAibiI6ICIzc2FyWmJsYnQwSGN2eTZJLUc2SE41MWRUZFdhYzY2V2ZoS2s1T2JLOUVPVXFkMEZwbXFBYlUtcnNIay1Bbjc3ai0zSVVVZEFfMG5EMy1BQy05NzlBZWlsUl9waXZHbzUwQ1RHLVFLWFBHN2NGc3RvYVhLSXkwQnpKM3VFNkVWRllKYmhpRWhqZXgxb0x0TUdpTDY1RkxNc2Nad24tY3E5RjI1dUlIMUhPdXY1V2FBRmRNVWdBUWVMdldPMDExSGZoWWhTc0xNekdfODc4NmZwRUxHeVdKNzgxYmFjZ3hzT2YxWXdxelJDVFQ3RXlKX1NZbF9SbUpTVXFwT3dOTkplckVOZ2tvaG9kMFQySkExV201SVFqb2VOMHNsZUR2Y2hBX1lEaHZTUTNRY0RNUElvN1A2STM0ekdVOWx2ZlBCb1VMdjJ6LXVsV1R2LTFrZVNwSDRaR2lXUDdoQTNhNjdQUXFfNkJ2RjQ2QURhc3RHTDlPSlRDSkl3MzB4MFNfc1UzLXc5eWRsRm9iN3JyS0tLTGlYQVhLNjJPMXM4T1JoZ1BqeDBWTGRKTmdTMXBiam9ETUpoTnk5M09RallzVE9VbXUzejgzc3VvTE0xdTg0R3FNV3Y0Umk1emt1S2tnbFZvY2xQTmYtaXlHRnhBV2dSOHp4RlVOUUtzLWVYX3ZqMlhnU2pCNVVFVTl2WVRFaklXbFZDRllRUTVsVDN3WWNNM09vakVySVhsWldremNTWXdfcnpSb0dPS1NMYVg3R0NRYUNyZmVEQmwzMmJDZW1KU09YeGhkWFJKTTVUdjVUZ2tvMndVMG95SFhCaldURjBkbUNTU1Z1TDVfck5wXzctSUJWTHNTQmtXOXJZRmhJdVVKd3ZQaC11QkN4WlVYNUc5SG9VX1N1N3VBRSIsCiAgIm90aCI6IFtdLAogICJwIjogIjY2djdsZGZyRllxekRjT2NseVUtUWFHWGxRQUs3N2htZTVrZTN2Sng3MW9JeUhqb2dZTkJRWm5fZ3NJS2s5VDJTaEdkYXdpS2RlcjI0alZEQURtd0tCcENKSHJpSVkzOVNVa2ZYSkh5clBzek9MaEN5T0haRDVOWXFveTRUMUlIa09ManA0MVpubzgzaFl1MmRCbWhWRndfSmotX042RXJRVVBiVk0wdGRQUFExVjd3N1FPUlZlTWZsSHY1WDdOMWR2VDFLZzlzTzlsU1FaTmdqUnhTeGpzOVpxbExHX2JBV09nMVR6bmt6RVdidnZaSGRFMW5DWVZORHI0c3NtZ1ZHcS1Wa1lUencxUEpRczVOdDg4S3RhcmU1MmsxbTd3eEVoeDRyZHloOTIwRU5NNW9GdUxnUTZTY1o0M1FMRFhlczlSZUNlR0ZGNW8tQ3lta282QnNldyIsCiAgInEiOiAiOGYzdGNNSkdmcEYwd0ZlTldIdHBfdGNYeDBtbVBvZGczOWdybkNfQm5DTk9rbG1xYjk3ZWxIYzltcW1yWVU1R1BjcVlMUExpLVlKQ0RLLVo0X3hTcVZHcXNCYTA5a0RhR0pHNm9KcXZXS3JXRmpEQmdPb0ZvbS1wRlhPNlBORTZjTjJRcEtwRjZTMkRMNUNENHhGMHBBYmRrTm05NS1LNGZKQkFsTlhfdTdkYXhjcmNQby1UdktiYk9OZlg4NkdVb2ZfcXI4cmtVazR6cm9kSkh6WXdZXzlVWVN5VDFVcjJBdUQyckRRQUs5akttT2tObWxKUzVRdlhsSW9fUGdOd0NnNW4yN29pVHAyZnk5ZFpHdmljRU9oc21SNkwwNjlyOUROdl80UHYzTG9DY3V4Y3ZaRjFTNDhuek5sOTZxeDJQOHVzakdTTjNIcXRzeUpYMHpRNnN3IiwKICAicWkiOiAiRUpLVnJqMTBkSzNMLWxuczZmc2V3LUdvRGg4a01lb2lBb184SE10Xy1MQi1wY3h1WFAxd1V4SWRMZ3VKNlNqbHRnR0VXdDZxM0NoR0oxYlhycmRxOF9ZNzRja25JUVY2ZEVPejlqN1VXWkdPVXVwUXduMExYRG14clpfU0tIZTBqb24xM1JtekxyZ1ZtZEIxVDlBSHFRSkRyMzZqdnhWYm1nV2pFemkxS080OHBlTHI5dWdrcHhwUWVrSjJ2RmtCdmJPOXM0eE14OG9LbV84RXFRQ0RVbE0xUDBLVXB1NVl1TkdhcnI2YzAtOURuS2EwdVExTU1IZkZpR0JucDVmWl9Sd1EzLTVTUHF4Unhac01Fb3V1bDB3MVA4S3FNQ0sxZEJHWjNjUlFwOWNsY05jRkFrVUlibWkwTVhiM0phaERBVkt0aHB0aWk0QW5vaXhmdjNwNUJRIiwKICAieDVjIjogW10KfQ==";


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
