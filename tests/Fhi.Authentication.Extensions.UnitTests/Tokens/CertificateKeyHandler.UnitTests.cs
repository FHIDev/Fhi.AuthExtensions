using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using Fhi.Authentication.Tokens;
using Microsoft.Extensions.DependencyInjection;

namespace Fhi.Authentication.Extensions.UnitTests.Tokens
{
    [TestFixture]
    public class CertificateKeyHandlerUnitTests
    {
        private const int RsaKeySize = 2048;
        private const string TestCertSubject = "CN=UnitTestCert";

        [Test]
        public void GetPrivateJwk_WithThumbprint_ReturnsJwk()
        {
            var thumbprint = Guid.NewGuid().ToString();
            using var rsa = RSA.Create(RsaKeySize);
            var certificate = CreateSelfSignedCertificate(TestCertSubject, rsa);

            var provider = new FakeCertificateProvider(certificate, rsa, thumbprint);
            var sut = new PrivateKeyHandler(provider);

            var jwkString = sut.GetPrivateJwk(thumbprint);

            AssertValidJwk(jwkString);
            certificate.Dispose();
        }

        [Test]
        public void GetPrivateJwk_WithPemInput_ReturnsJwk()
        {
            using var rsa = RSA.Create(RsaKeySize);
            var pem = rsa.ExportRSAPrivateKeyPem();

            var provider = new FakeCertificateProvider(null, null, null);
            var sut = new PrivateKeyHandler(provider);

            var jwkString = sut.GetPrivateJwk(pem);

            AssertValidJwk(jwkString);
        }

        [Test]
        public void GetPrivateJwk_WithJwkInput_ReturnsJwk()
        {
            using var rsa = RSA.Create(RsaKeySize);
            var pem = rsa.ExportRSAPrivateKeyPem();
            var expectedJwk = PrivateJwk.ParseFromPem(pem);
            string jwkInput = expectedJwk; // implicit conversion

            var provider = new FakeCertificateProvider(null, null, null);
            var sut = new PrivateKeyHandler(provider);

            var jwkString = sut.GetPrivateJwk(jwkInput);

            AssertValidJwk(jwkString);
            Assert.That(jwkString, Is.EqualTo(jwkInput));
        }

        [Test]
        public void GetPrivateJwk_WithBase64Input_ReturnsJwk()
        {
            using var rsa = RSA.Create(RsaKeySize);
            var pem = rsa.ExportRSAPrivateKeyPem();
            var jwk = PrivateJwk.ParseFromPem(pem);
            string jwkJson = jwk;
            var base64Input = Convert.ToBase64String(Encoding.UTF8.GetBytes(jwkJson));

            var provider = new FakeCertificateProvider(null, null, null);
            var sut = new PrivateKeyHandler(provider);

            var jwkString = sut.GetPrivateJwk(base64Input);

            AssertValidJwk(jwkString);
        }

        [Test]
        public void GetPrivateJwk_WithEmptyInput_ThrowsArgumentNullException()
        {
            var provider = new FakeCertificateProvider(null, null, null);
            var sut = new PrivateKeyHandler(provider);

            var ex = Assert.Throws<ArgumentNullException>(() => sut.GetPrivateJwk(string.Empty));
            Assert.That(ex.ParamName, Is.EqualTo("secretOrThumbprint"));
        }

        [Test]
        public void GetPrivateJwk_WithInvalidThumbprint_ThrowsInvalidOperationException()
        {
            var provider = new FakeCertificateProvider(null, null, null);
            var sut = new PrivateKeyHandler(provider);

            var ex = Assert.Throws<InvalidOperationException>(() => 
                sut.GetPrivateJwk(Guid.NewGuid().ToString()));

            Assert.That(ex.Message, Does.Contain("No certificate found"));
        }

        [Test]
        public void GetPrivateJwk_WithCertificateLackingPrivateKey_ThrowsInvalidOperationException()
        {
            const string thumbprint = "THUMB123";
            using var rsa = RSA.Create(RsaKeySize);
            var certWithPrivateKey = CreateSelfSignedCertificate("CN=PublicOnly", rsa);
            
            var certBytes = certWithPrivateKey.Export(X509ContentType.Cert);
            certWithPrivateKey.Dispose();
            
#if NET9_0_OR_GREATER
            var publicOnly = X509CertificateLoader.LoadCertificate(certBytes);
#else
            var publicOnly = new X509Certificate2(certBytes);
#endif

            var provider = new FakeCertificateProvider(publicOnly, null, thumbprint);
            var sut = new PrivateKeyHandler(provider);

            var ex = Assert.Throws<InvalidOperationException>(() => 
                sut.GetPrivateJwk(thumbprint));

            Assert.That(ex.Message, Does.Contain("has no private key"));

            publicOnly.Dispose();
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

        private static void AssertValidJwk(string jwkString)
        {
            Assert.That(jwkString, Is.Not.Null.And.Not.Empty);
            
            JsonDocument? doc = null;
            Assert.DoesNotThrow(() => doc = JsonDocument.Parse(jwkString), "JWK should be valid JSON");
            
            Assert.That(doc, Is.Not.Null);
            var root = doc!.RootElement;
            Assert.That(root.TryGetProperty("kty", out _), Is.True, "JWK should have 'kty' property");
            Assert.That(root.TryGetProperty("n", out _), Is.True, "RSA JWK should have 'n' (modulus) property");
            Assert.That(root.TryGetProperty("e", out _), Is.True, "RSA JWK should have 'e' (exponent) property");
            Assert.That(root.TryGetProperty("d", out _), Is.True, "Private JWK should have 'd' property");
            
            doc.Dispose();
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
