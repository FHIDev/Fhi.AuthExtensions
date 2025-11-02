using Duende.AccessTokenManagement;
using Fhi.Authentication.ClientCredentials;
using Fhi.Authentication.OpenIdConnect;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NSubstitute;

namespace Fhi.Authentication.Extensions.UnitTests.ClientCredentials
{
    public class ClientCredentialsExtensionsTests
    {
        [Test]
        public void GIVEN_addClientCredentialsClientOptions_WHEN_SharedSecretAndValidOptions_THEN_success()
        {
            var namedOption = "nameOnOption";
            var authority = "https://authority";
            var tokenEndpoint = "https://tokenendpoint";
            var clientCredentialsOption = new ServiceCollection()
                .AddSingleton(CreateDiscoveryStoreMock(authority, "issuer", tokenEndpoint))
                .AddClientCredentialsClientOptions(
                    namedOption,
                    authority,
                    "ClientId",
                    SharedSecret.Parse("secret"),
                    "scope1");

            var options = clientCredentialsOption.CreateServiceProvider().GetClientCredentialsOption(namedOption);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(options.ClientId.ToString(), Is.EqualTo("ClientId"), "");
                Assert.That(options.Scope.ToString(), Is.EqualTo("scope1"), "");
                Assert.That(options.TokenEndpoint, Is.EqualTo(tokenEndpoint), "");
            }
        }

        [Test]
        public void GIVEN_addClientCredentialsClientOptions_WHEN_PrivateJwkSecretAndValidOptions_THEN_success()
        {
            var namedOption = "nameOnOption";
            var authority = "https://authority";
            var tokenEndpoint = "https://tokenendpoint";
            var issuer = "https://issuer";
            var clientCredentialsOption = new ServiceCollection()
                .AddSingleton(CreateDiscoveryStoreMock(authority, issuer, tokenEndpoint))
                .AddClientCredentialsClientOptions(
                    namedOption,
                    authority,
                    "ClientId",
                    PrivateJwk.ParseFromBase64Encoded("ewogICJhbGciOiAiUlM1MTIiLAogICJkIjogIjJoRnZSYXdqODBjdFpwZU96R0NsU19ObW1nSTFwbmVJS1BEbktJb2lzT2hXWmRCTTc1SGhGUi1mMktkVjQtdVpKRktHZThsYl9vbWVpMDhrbkppeTRXTnlKdnNhRzRXQUFoZC1EZEpUYUh5OFJEUG9pUV9zaDhrVzc5Y2hTcHhFUlVCc2xKTUhqa1JDNzFMeVdua1hnd1BBeFpVdUxINGdpd2MyRmVlTUNrbGpWREFvSngydkxsTE1ueDh5cm5NZWNmXzNzYkE0bjNkQW9Fb0ZBcXB2QWdEZlRmUHJVUXJ6TGdLWXNYMy1TTTdnb2VLUWRtdUZKLTZKZUZBSmRDdFpNUmJqT1l6OWRZS3p6cWFmMkxrcUs5clZJVGlzTWNvSEwzRnFFbmprTlBmZ1Q5RTc3b0ZhU2puckNia1dRUjZ6ZmhRQWpaY0Z6RXZHTU1ydTZNWG1tcllHbTNmRnlGTEtiODRaOGcwVU9aT0FYaEl5UTBQS2RWTzZSVnFCR2hwaUl2VDJGbEQzaXZyOGw1MmZMZGVvZHdpUThvTW1zalUzYmtGMTVyMWZWZ2NtYXk1WTAtV2NVdHRaN2tfbXBSVTVLcUtjZVhmR1RGSVA3Y0E4cEdmQkZ4bk9EQ0s2ZDFnVTdGNEJhZ2FHNU1XaEtfTnZ3MjV5aU8zdGt1cXM4NDAwenF3Z1Z1VlFQWlFnYmZLREtIb1NKOU01STc3d0d0NEk0Ukx2bUo1bFFwcVM2eFdKVWRvalRnOHF2UVFTVkMtVlo5bml2ekt5bzhhWTBTM3I0RjhXUlNjUXNNTl9EazkwX1JHMnlRdzcwdHgzSkdfRHhYc3BlQmdYR3RvWHBsTlpLekx0WWVFVG01WF95NE5rZ3pzcEJqWnhsNWNvVDFmSGtuVF9WUW5BQmVFIiwKICAiZHAiOiAiSkJNX2Y4QjlpOTZmYWd2M2ZHdUhEQ1RnUHo2eWJkLWNVRzh2LXBEQ1FCRllKdGZiUDNnSUZUS3RtaVo1MkFkUmJDVW1JNnc3QmJIaC1ONk9Vd3FCbktIb1hnMHhjRkY3NUN2OVVQNUdWNVJjVXVKaDFyNDdxOWF5Uy1rZWpZWFJBcXk0akgxbXFvN2h3Ti1TSGxPT0RvdlFYdHFET3REQV9reVUxblpSN3dVamdvWDEtMUlBZlZVM0VGT3A2S3hmM3NYQWdaLTZ3YTZTbU5jbWdITkZlSEs1QTI0R3dWRVVRSWZMSVVXTGNIbDNUZFFGUEJoZEhXZjZVN2Rvd0NhT2xIY3NQZ3BNQmlwZE5JeV8tTk1Jb0taM1dKVVNUN1NQRG1hdFVtX2pteXhrNkhCd0hwQzhJNTZmRW5yeTB0R3NOWHJjcWthTFNRR2pYYklTTWJuczRRIiwKICAiZHEiOiAieFdRTjFGYUVUaVcwQ0ZqaVVQU0taM2RvVEFVRmVDZlJtNFduUFFXdmRmOU96ZzRaLXByZkVGZWJocGN6MW5HVTFqNTFsdVNneEVtLU9yN2FWcC14aGEtNV9Wem9TNTVteUhmR3RjNjNqU244NjZqbGpwUlYtRVFBdGlEcUgwWEQxLUREWmE0R3pNZDFKTTFLNTJPTHMyaHVQd045NXJLSExnUTB5Yk10b1I5VkNkWUpoWFowV2dQNC1nc0dlaEtwUE9PVnVjTlVMdGJmVFZQSkQ0NlNObDlGb3VaTHlFWVhHZzRLNkRpYWt5bUpTT1dCVlFvbDFfT3E2OTFBN1FLODExY2MzSnlkODJhYnZCaHk5SThxTHBpSVRQWWZ0Sko1Q244U3NtZ2F0Zjk4ay1XOVdGeFlRMFFJZ1E0X3FiYjMxZDM3WjZGMkhneHJ0NTBoNm9QR0JRIiwKICAiZSI6ICJBUUFCIiwKICAia2V5X29wcyI6IFtdLAogICJraWQiOiAiYXV0aGV4dGVuc2lvbi5zYW1wbGUiLAogICJrdHkiOiAiUlNBIiwKICAibiI6ICIzc2FyWmJsYnQwSGN2eTZJLUc2SE41MWRUZFdhYzY2V2ZoS2s1T2JLOUVPVXFkMEZwbXFBYlUtcnNIay1Bbjc3ai0zSVVVZEFfMG5EMy1BQy05NzlBZWlsUl9waXZHbzUwQ1RHLVFLWFBHN2NGc3RvYVhLSXkwQnpKM3VFNkVWRllKYmhpRWhqZXgxb0x0TUdpTDY1RkxNc2Nad24tY3E5RjI1dUlIMUhPdXY1V2FBRmRNVWdBUWVMdldPMDExSGZoWWhTc0xNekdfODc4NmZwRUxHeVdKNzgxYmFjZ3hzT2YxWXdxelJDVFQ3RXlKX1NZbF9SbUpTVXFwT3dOTkplckVOZ2tvaG9kMFQySkExV201SVFqb2VOMHNsZUR2Y2hBX1lEaHZTUTNRY0RNUElvN1A2STM0ekdVOWx2ZlBCb1VMdjJ6LXVsV1R2LTFrZVNwSDRaR2lXUDdoQTNhNjdQUXFfNkJ2RjQ2QURhc3RHTDlPSlRDSkl3MzB4MFNfc1UzLXc5eWRsRm9iN3JyS0tLTGlYQVhLNjJPMXM4T1JoZ1BqeDBWTGRKTmdTMXBiam9ETUpoTnk5M09RallzVE9VbXUzejgzc3VvTE0xdTg0R3FNV3Y0Umk1emt1S2tnbFZvY2xQTmYtaXlHRnhBV2dSOHp4RlVOUUtzLWVYX3ZqMlhnU2pCNVVFVTl2WVRFaklXbFZDRllRUTVsVDN3WWNNM09vakVySVhsWldremNTWXdfcnpSb0dPS1NMYVg3R0NRYUNyZmVEQmwzMmJDZW1KU09YeGhkWFJKTTVUdjVUZ2tvMndVMG95SFhCaldURjBkbUNTU1Z1TDVfck5wXzctSUJWTHNTQmtXOXJZRmhJdVVKd3ZQaC11QkN4WlVYNUc5SG9VX1N1N3VBRSIsCiAgIm90aCI6IFtdLAogICJwIjogIjY2djdsZGZyRllxekRjT2NseVUtUWFHWGxRQUs3N2htZTVrZTN2Sng3MW9JeUhqb2dZTkJRWm5fZ3NJS2s5VDJTaEdkYXdpS2RlcjI0alZEQURtd0tCcENKSHJpSVkzOVNVa2ZYSkh5clBzek9MaEN5T0haRDVOWXFveTRUMUlIa09ManA0MVpubzgzaFl1MmRCbWhWRndfSmotX042RXJRVVBiVk0wdGRQUFExVjd3N1FPUlZlTWZsSHY1WDdOMWR2VDFLZzlzTzlsU1FaTmdqUnhTeGpzOVpxbExHX2JBV09nMVR6bmt6RVdidnZaSGRFMW5DWVZORHI0c3NtZ1ZHcS1Wa1lUencxUEpRczVOdDg4S3RhcmU1MmsxbTd3eEVoeDRyZHloOTIwRU5NNW9GdUxnUTZTY1o0M1FMRFhlczlSZUNlR0ZGNW8tQ3lta282QnNldyIsCiAgInEiOiAiOGYzdGNNSkdmcEYwd0ZlTldIdHBfdGNYeDBtbVBvZGczOWdybkNfQm5DTk9rbG1xYjk3ZWxIYzltcW1yWVU1R1BjcVlMUExpLVlKQ0RLLVo0X3hTcVZHcXNCYTA5a0RhR0pHNm9KcXZXS3JXRmpEQmdPb0ZvbS1wRlhPNlBORTZjTjJRcEtwRjZTMkRMNUNENHhGMHBBYmRrTm05NS1LNGZKQkFsTlhfdTdkYXhjcmNQby1UdktiYk9OZlg4NkdVb2ZfcXI4cmtVazR6cm9kSkh6WXdZXzlVWVN5VDFVcjJBdUQyckRRQUs5akttT2tObWxKUzVRdlhsSW9fUGdOd0NnNW4yN29pVHAyZnk5ZFpHdmljRU9oc21SNkwwNjlyOUROdl80UHYzTG9DY3V4Y3ZaRjFTNDhuek5sOTZxeDJQOHVzakdTTjNIcXRzeUpYMHpRNnN3IiwKICAicWkiOiAiRUpLVnJqMTBkSzNMLWxuczZmc2V3LUdvRGg4a01lb2lBb184SE10Xy1MQi1wY3h1WFAxd1V4SWRMZ3VKNlNqbHRnR0VXdDZxM0NoR0oxYlhycmRxOF9ZNzRja25JUVY2ZEVPejlqN1VXWkdPVXVwUXduMExYRG14clpfU0tIZTBqb24xM1JtekxyZ1ZtZEIxVDlBSHFRSkRyMzZqdnhWYm1nV2pFemkxS080OHBlTHI5dWdrcHhwUWVrSjJ2RmtCdmJPOXM0eE14OG9LbV84RXFRQ0RVbE0xUDBLVXB1NVl1TkdhcnI2YzAtOURuS2EwdVExTU1IZkZpR0JucDVmWl9Sd1EzLTVTUHF4Unhac01Fb3V1bDB3MVA4S3FNQ0sxZEJHWjNjUlFwOWNsY05jRkFrVUlibWkwTVhiM0phaERBVkt0aHB0aWk0QW5vaXhmdjNwNUJRIiwKICAieDVjIjogW10KfQ=="),
                    "scope1");

            var provider = clientCredentialsOption.CreateServiceProvider();
            var options = provider.GetClientCredentialsOption(namedOption);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(options.ClientId.ToString(), Is.EqualTo("ClientId"), "");
                Assert.That(options.Scope.ToString(), Is.EqualTo("scope1"), "");
                Assert.That(options.TokenEndpoint, Is.EqualTo(tokenEndpoint), "");
            }

            var clientAssertionOptions = provider.GetClientAssertionOption(namedOption);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(clientAssertionOptions.Issuer, Is.EqualTo(issuer), "");
                Assert.That(clientAssertionOptions.ClientAssertionType, Is.EqualTo("urn:ietf:params:oauth:client-assertion-type:jwt-bearer"), "");
            }
            var jsonWebKey = new JsonWebKey(clientAssertionOptions.PrivateJwk.ToString());
            Assert.That(jsonWebKey.Kid, Is.EqualTo("authextension.sample"));
        }

        [Test]
        public void GIVEN_addClientCredentialsClientOptions_and_ParseFromBase64Encoded_WHEN_validEscapedJson_THEN_success()
        {
            var jsonEscaped = CreateConfig("ClientCredentials\\appsettings.ClientCredentialsTests.json")
                .GetValue<string>("Keys:PrivateJwkJsonEscaped");

            var clientCredentialsOption = CreateServiceCollectionWithDiscoveryStoreMock()
            .AddClientCredentialsClientOptions(
                "Name",
                "authority",
                "ClientId",
                PrivateJwk.ParseFromJson(jsonEscaped!),
                "scope");

            var options = clientCredentialsOption.Services.BuildServiceProvider().GetClientAssertionOption("Name");
            var jsonWebKey = new JsonWebKey(options.PrivateJwk.ToString());
            Assert.That(jsonWebKey.Kid, Is.EqualTo("authextension.sample"));
        }

        [Test]
        public void GIVEN_addClientCredentialsClientOptions_and_ParseFrom_WHEN_validstring_THEN_success()
        {
            var base64Encoded = CreateConfig("ClientCredentials\\appsettings.ClientCredentialsTests.json")
                .GetValue<string>("Keys:PrivateJwkBase64");

            var clientCredentialsOption = CreateServiceCollectionWithDiscoveryStoreMock()
            .AddClientCredentialsClientOptions(
                "Name",
                "authority",
                "ClientId",
                PrivateJwk.ParseFromBase64Encoded(base64Encoded!),
                "scope");

            var options = clientCredentialsOption!.Services!.BuildServiceProvider().GetClientAssertionOption("Name");
            var jsonWebKey = new JsonWebKey(options.PrivateJwk.ToString());
            Assert.That(jsonWebKey.Kid, Is.EqualTo("authextension.sample"));
        }


        private static IServiceCollection CreateServiceCollectionWithDiscoveryStoreMock()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IDiscoveryDocumentStore>(Substitute.For<IDiscoveryDocumentStore>());
            return services;
        }

        private static IConfigurationRoot CreateConfig(string fileName)
        {
            return new ConfigurationBuilder()
                        .SetBasePath(AppContext.BaseDirectory)
                        .AddJsonFile(fileName, optional: false, reloadOnChange: false)
                        .Build();
        }

        private static IDiscoveryDocumentStore CreateDiscoveryStoreMock(string authority, string issuer = "https://issuer", string tokenEndpoint = "https://token")
        {
            var discoveryDocMock = Substitute.For<IDiscoveryDocumentStore>();
            discoveryDocMock.Get(authority).Returns(new DiscoveryDocument(authority, issuer, null, tokenEndpoint, null, null, null));
            return discoveryDocMock;
        }
    }

    internal static class TestExtensions
    {

        public static ServiceProvider CreateServiceProvider(this ClientCredentialsOptionBuilder clientCredentialsOption)
        {
            return clientCredentialsOption!.Services!.BuildServiceProvider();
        }

        public static ClientAssertionOptions GetClientAssertionOption(this ServiceProvider provider, string name)
        {
            var options = provider.GetRequiredService<IOptionsMonitor<ClientAssertionOptions>>().Get(name);
            return options;
        }

        public static ClientCredentialsClient GetClientCredentialsOption(this ServiceProvider provider, string name)
        {
            var options = provider.GetRequiredService<IOptionsMonitor<ClientCredentialsClient>>().Get(name);
            return options;
        }
    }
}
