using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using Fhi.Authentication.Tokens;
using Microsoft.IdentityModel.Tokens;

namespace Fhi.Authentication.Extensions.UnitTests.Tokens
{
    [TestFixture]
    public class CertificateKeyHandlerUnitTests
    {
        private const int RsaKeySize = 2048;
        private const string TestCertSubject = "CN=UnitTestCert";

        [Test]
        public void GetPrivateKeyAsJwk_WithValidCertificate_ReturnsPrivateJwk()
        {
            var thumbprint = Guid.NewGuid().ToString();
            using var rsa = RSA.Create(RsaKeySize);
            using var certificate = CreateSelfSignedCertificate(TestCertSubject, rsa);

            var provider = new FakeCertificateProvider(certificate, rsa, thumbprint);
            var sut = new CertificateKeyHandler(provider);

            var jwkJson = sut.GetPrivateKeyAsJwk(thumbprint);

            AssertValidPrivateJwk(jwkJson, certificate.Thumbprint);
        }

        [Test]
        public void GetPrivateKeyAsJwk_WhenCertificateNotFound_ThrowsInvalidOperationException()
        {
            var provider = new FakeCertificateProvider(null, null, null);
            var sut = new CertificateKeyHandler(provider);

            var ex = Assert.Throws<InvalidOperationException>(() => 
                sut.GetPrivateKeyAsJwk(Guid.NewGuid().ToString()));

            Assert.That(ex.Message, Does.Contain("No certificate found"));
        }

        [Test]
        public void GetPrivateKeyAsJwk_WhenCertificateLacksPrivateKey_ThrowsInvalidOperationException()
        {
            const string thumbprint = "THUMB123";
            using var rsa = RSA.Create(RsaKeySize);
            using var certificate = CreateSelfSignedCertificate("CN=PublicOnly", rsa);

            var provider = new FakeCertificateProvider(certificate, null, thumbprint);
            var sut = new CertificateKeyHandler(provider);

            var ex = Assert.Throws<InvalidOperationException>(() => 
                sut.GetPrivateKeyAsJwk(thumbprint));

            Assert.That(ex.Message, Does.Contain("has no private key"));
        }

        private static X509Certificate2 CreateSelfSignedCertificate(string subject, RSA rsa)
        {
            var request = new CertificateRequest(
                subject, 
                rsa, 
                HashAlgorithmName.SHA256, 
                RSASignaturePadding.Pkcs1);

            return request.CreateSelfSigned(
                DateTime.UtcNow.AddDays(-1), 
                DateTime.UtcNow.AddDays(365));
        }

        private static void AssertValidPrivateJwk(string jwkJson, string expectedKid)
        {
            Assert.That(jwkJson, Is.Not.Null.And.Not.Empty);

            var jwk = JsonSerializer.Deserialize<JsonWebKey>(jwkJson);

            Assert.Multiple(() =>
            {
                Assert.That(jwk, Is.Not.Null);
                Assert.That(jwk?.Kty, Is.EqualTo("RSA"));
                Assert.That(jwk?.D, Is.Not.Null.And.Not.Empty, "Private key component D should be present");
                Assert.That(jwk?.Kid, Is.EqualTo(expectedKid));
            });
        }

        private class FakeCertificateProvider(X509Certificate2? cert, RSA? rsa, string? thumb) : ICertificateProvider
        {
            public X509Certificate2? GetCertificate(string thumbprint) =>
                MatchesThumbprint(thumbprint) ? cert : null;

            public RSA? GetPrivateKey(string thumbprint) =>
                MatchesThumbprint(thumbprint) ? rsa : null;

            private bool MatchesThumbprint(string thumbprint) =>
                thumb != null && string.Equals(thumbprint, thumb, StringComparison.OrdinalIgnoreCase);
        }
    }
}
