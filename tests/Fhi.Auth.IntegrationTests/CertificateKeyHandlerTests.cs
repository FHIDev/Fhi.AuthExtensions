using System.Security.Cryptography.X509Certificates;
using Fhi.Auth.IntegrationTests.Setup;
using Fhi.Authentication;
using Fhi.Authentication.Tokens;
using Microsoft.Extensions.DependencyInjection;

#if NET9_0_OR_GREATER
namespace Fhi.Auth.IntegrationTests
{
    [TestFixture]
    [Category("RequiresLocalCertificateStore")]
    public class CertificateKeyHandlerTests
    {
        private IServiceProvider? _serviceProvider;
        private ICertificateKeyHandler? _certificateKeyHandler;
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

            _certificateKeyHandler = _serviceProvider.GetRequiredService<ICertificateKeyHandler>();
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
        public void GIVEN_ValidCertificateInStore_When_GetPrivateKeyAsJwk_Then_ReturnsPrivateJwk()
        {
            // Given: create and install a certificate with private key
            var cert = CreateAndInstallCertificate();

            // When
            var jwk = _certificateKeyHandler!.GetPrivateKeyAsJwk(cert.Thumbprint);

            // Then
            Assert.That(jwk, Is.Not.Null.Or.Empty);
            TestContext.Progress.WriteLine("Test JWK: " + jwk);
            Assert.That(jwk, Does.Contain("\"kty\":\"RSA\""));
            Assert.That(jwk, Does.Contain("\"kid\":"));
            Assert.That(jwk, Does.Contain("\"d\":"));

            cert.Dispose();
        }

        [Test]
        public void GIVEN_MissingCertificate_When_GetPrivateKeyAsJwk_Then_ThrowsException()
        {
            var missingThumb = Guid.NewGuid().ToString("N");

            var exception = Assert.Throws<InvalidOperationException>(() => _certificateKeyHandler!.GetPrivateKeyAsJwk(missingThumb));
            Assert.That(exception.Message, Does.Contain("No certificate found"));
        }

        [Test]
        public void GIVEN_EmptyThumbprint_When_GetPrivateKeyAsJwk_Then_ThrowsArgumentNullException()
        {
            var exception = Assert.Throws<ArgumentNullException>(() => _certificateKeyHandler!.GetPrivateKeyAsJwk(string.Empty));
            Assert.That(exception.ParamName, Is.EqualTo("certificateThumbprint"));
        }

        [Test]
        public void GIVEN_ExpiredCertificateInStore_When_GetPrivateKeyAsJwk_Then_ThrowsException()
        {
            var cert = CreateAndInstallExpiredCertificate();

            var exception = Assert.Throws<InvalidOperationException>(() => _certificateKeyHandler!.GetPrivateKeyAsJwk(cert.Thumbprint));
            Assert.That(exception.Message, Does.Contain("has expired"));

            cert.Dispose();
        }

        [Test]
        public void GIVEN_CertificateWithoutPrivateKeyInStore_When_GetPrivateKeyAsJwk_Then_ThrowsException()
        {
            var cert = new TestCertificateBuilder()
                .WithSubject("CN=PublicOnlyTestCert")
                .PublicOnly()
                .Build();

            // install public-only cert (DER) directly
            InstallCertificate(cert);
            _thumbprintsToCleanup.Add(cert.Thumbprint);

            var exception = Assert.Throws<InvalidOperationException>(() => _certificateKeyHandler!.GetPrivateKeyAsJwk(cert.Thumbprint));
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
