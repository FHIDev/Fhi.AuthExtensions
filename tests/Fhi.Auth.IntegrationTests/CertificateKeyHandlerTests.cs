using System.Security.Cryptography.X509Certificates;
using Fhi.Auth.IntegrationTests.Setup;
using Fhi.Authentication;
using Fhi.Authentication.Tokens;
using Microsoft.Extensions.DependencyInjection;

#if NET9_0_OR_GREATER
namespace Fhi.Auth.IntegrationTests
{
    [TestFixture]
    [Explicit ("Requires Windows and Local Machine Certificate Store access")]
    public class CertificateKeyHandlerTests
    {
        private IServiceProvider? _serviceProvider;
        private IPrivateKeyHandler? _certificateKeyHandler;
        private readonly List<string> _thumbprintsToCleanup = new();

        /// <summary>
        /// Clean up any leftover test certificates from previous test runs (in case tests crashed or were interrupted).
        /// </summary>
        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            CleanupTestCertificates();
        }

        [SetUp]
        public void Setup()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddCertificateStoreKeyHandler();

            _serviceProvider = services.BuildServiceProvider();

            _certificateKeyHandler = _serviceProvider.GetRequiredService<IPrivateKeyHandler>();
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
                store.Open(OpenFlags.ReadWrite);

                foreach (var thumb in _thumbprintsToCleanup)
                {
                    var found = store.Certificates.Find(X509FindType.FindByThumbprint, thumb, false);
                    foreach (var c in found)
                    {
                        try { store.Remove(c); } catch { /* best-effort cleanup */ }
                    }
                }

                store.Close();
            }
            finally
            {
                (_serviceProvider as IDisposable)?.Dispose();
                _thumbprintsToCleanup.Clear();
            }
        }

        /// <summary>
        /// Clean up all test certificates from the store (ensures leftover certs from crashed tests are removed).
        /// </summary>
        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            CleanupTestCertificates();
        }

        [Test]
        public void GIVEN_ValidCertificateInStore_When_GetPrivateJwk_WithThumbprint_Then_ReturnsJwk()
        {
            var cert = CreateAndInstallCertificate();
            var jwkString = _certificateKeyHandler!.GetPrivateJwk(cert.Thumbprint);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(jwkString, Is.Not.Null.Or.Empty);
                TestContext.Progress.WriteLine("Test JWK: " + jwkString);
                
                Assert.DoesNotThrow(() => System.Text.Json.JsonDocument.Parse(jwkString));
                
                Assert.That(jwkString, Does.Contain("\"kty\""));
                Assert.That(jwkString, Does.Contain("\"n\""));
                Assert.That(jwkString, Does.Contain("\"d\""));
            }

            cert.Dispose();
        }

        [Test]
        public void GIVEN_PemString_When_GetPrivateJwk_Then_ReturnsJwk()
        {
            var cert = CreateAndInstallCertificate();
            
            using var rsa = cert.GetRSAPrivateKey();
            var pem = rsa!.ExportRSAPrivateKeyPem();
            
            var jwkString = _certificateKeyHandler!.GetPrivateJwk(pem);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(jwkString, Is.Not.Null.Or.Empty);
                Assert.DoesNotThrow(() => System.Text.Json.JsonDocument.Parse(jwkString));
                Assert.That(jwkString, Does.Contain("\"kty\""));
            }

            cert.Dispose();
        }

        [Test]
        public void GIVEN_JwkString_When_GetPrivateJwk_Then_ReturnsSameJwk()
        {
            var cert = CreateAndInstallCertificate();

            var originalJwk = _certificateKeyHandler!.GetPrivateJwk(cert.Thumbprint);
            var resultJwk = _certificateKeyHandler!.GetPrivateJwk(originalJwk);

            Assert.That(resultJwk, Is.EqualTo(originalJwk));

            cert.Dispose();
        }

        [Test]
        public void GIVEN_Base64EncodedJwk_When_GetPrivateJwk_Then_ReturnsDecodedJwk()
        {
            var cert = CreateAndInstallCertificate();
            
            var jwkString = _certificateKeyHandler!.GetPrivateJwk(cert.Thumbprint);
            var base64Input = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(jwkString));
            var resultJwk = _certificateKeyHandler!.GetPrivateJwk(base64Input);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(resultJwk, Is.Not.Null.Or.Empty);
                Assert.DoesNotThrow(() => System.Text.Json.JsonDocument.Parse(resultJwk));
            }

            cert.Dispose();
        }

        [Test]
        public void GIVEN_MissingCertificate_When_GetPrivateJwk_Then_ThrowsException()
        {
            var missingThumb = Guid.NewGuid().ToString("N");

            var exception = Assert.Throws<InvalidOperationException>(() => _certificateKeyHandler!.GetPrivateJwk(missingThumb));
            Assert.That(exception.Message, Does.Contain("No certificate found"));
        }

        [Test]
        public void GIVEN_EmptyInput_When_GetPrivateJwk_Then_ThrowsArgumentNullException()
        {
            var exception = Assert.Throws<ArgumentNullException>(() => _certificateKeyHandler!.GetPrivateJwk(string.Empty));
            Assert.That(exception.ParamName, Is.EqualTo("secretOrThumbprint"));
        }

        [Test]
        public void GIVEN_ExpiredCertificate_When_GetPrivateJwk_Then_ThrowsException()
        {
            var cert = CreateAndInstallExpiredCertificate();

            var exception = Assert.Throws<InvalidOperationException>(() => _certificateKeyHandler!.GetPrivateJwk(cert.Thumbprint));
            Assert.That(exception.Message, Does.Contain("has expired"));

            cert.Dispose();
        }

        [Test]
        public void GIVEN_CertificateWithoutPrivateKey_When_GetPrivateJwk_Then_ThrowsException()
        {
            var cert = new TestCertificateBuilder()
                .WithSubject("CN=PublicOnlyTestCert")
                .PublicOnly()
                .Build();

            InstallCertificate(cert);
            _thumbprintsToCleanup.Add(cert.Thumbprint);

            var exception = Assert.Throws<InvalidOperationException>(() => _certificateKeyHandler!.GetPrivateJwk(cert.Thumbprint));
            Assert.That(exception.Message, Does.Contain("has no private key"));

            cert.Dispose();
        }

        // Helpers
        private void CleanupTestCertificates()
        {
            try
            {
                using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
                store.Open(OpenFlags.ReadWrite);

                // Remove all test certificates by looking for known test subject names
                var testSubjects = new[] { "CN=TestCert", "CN=PublicOnlyTestCert", "CN=ExpiredTestCert" };
                
                foreach (var subject in testSubjects)
                {
                    var found = store.Certificates.Find(X509FindType.FindBySubjectDistinguishedName, subject, false);
                    foreach (var cert in found)
                    {
                        try
                        {
                            store.Remove(cert);
                            TestContext.Progress.WriteLine($"Cleaned up leftover test certificate: {cert.Subject} (Thumbprint: {cert.Thumbprint})");
                        }
                        catch (Exception ex)
                        {
                            TestContext.Progress.WriteLine($"Failed to remove certificate {cert.Subject}: {ex.Message}");
                        }
                    }
                }

                store.Close();
            }
            catch (Exception ex)
            {
                TestContext.Progress.WriteLine($"Error during test certificate cleanup: {ex.Message}");
            }
        }

        private X509Certificate2 CreateAndInstallCertificate()
        {
            var builder = new TestCertificateBuilder().WithSubject("CN=TestCert");

            var cert = builder.Build();
            InstallCertificate(cert);
            _thumbprintsToCleanup.Add(cert.Thumbprint);
            return cert;
        }
        
        private X509Certificate2 CreateAndInstallExpiredCertificate()
        {
            var builder = new TestCertificateBuilder()
                .WithSubject("CN=ExpiredTestCert")
                .Expired();

            var cert = builder.Build();
            InstallCertificate(cert);
            _thumbprintsToCleanup.Add(cert.Thumbprint);
            return cert;
        }

        private void InstallCertificate(X509Certificate2 cert)
        {
            using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadWrite);
            store.Add(cert);
            store.Close();
        }
    }
}
#endif
