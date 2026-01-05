using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using Fhi.Authentication.Tokens;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace Fhi.Authentication.Extensions.UnitTests.Tokens
{
    public class PrivateKeyHandlerUnitTests
    {
        private const int RsaKeySize = 2048;
        private const string TestCertSubject = "CN=UnitTestCert";

        [Test]
        public void GetPrivateJwk_WithThumbprint_ReturnsJwk()
        {
            var thumbprint = Guid.NewGuid().ToString();
            using var rsa = RSA.Create(RsaKeySize);
            using var certificate = CreateSelfSignedCertificate(TestCertSubject, rsa);

            var provider = new FakeCertificateProvider(certificate, rsa, thumbprint);
            var sut = new PrivateKeyHandler(provider);

            var jwkString = sut.GetPrivateJwk(thumbprint);

            AssertValidJwk(jwkString);
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
            using var certWithPrivateKey = CreateSelfSignedCertificate("CN=PublicOnly", rsa);

            var certBytes = certWithPrivateKey.Export(X509ContentType.Cert);

#if NET9_0_OR_GREATER
            using var publicOnly = X509CertificateLoader.LoadCertificate(certBytes);
#else
            using var publicOnly = new X509Certificate2(certBytes);
#endif

            var provider = new FakeCertificateProvider(publicOnly, null, thumbprint);
            var sut = new PrivateKeyHandler(provider);

            var ex = Assert.Throws<InvalidOperationException>(() =>
                sut.GetPrivateJwk(thumbprint));

            Assert.That(ex.Message, Does.Contain("has no private key"));
        }

        [Test]
        public void GetPrivateJwk_WithExpiredCertificate_ThrowsInvalidOperationException()
        {
            var fakeTime = new FakeTimeProvider(new DateTimeOffset(2025, 6, 1, 12, 0, 0, TimeSpan.Zero));
            var thumbprint = "EXPIRED_CERT";
            using var rsa = RSA.Create(RsaKeySize);
            using var expiredCert = CreateSelfSignedCertificateWithDates(
                "CN=ExpiredCert",
                rsa,
                notBefore: new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero),
                notAfter: new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero));
            var provider = new FakeCertificateProvider(expiredCert, rsa, thumbprint);
            var sut = new PrivateKeyHandler(provider, fakeTime);

            var ex = Assert.Throws<InvalidOperationException>(() => sut.GetPrivateJwk(thumbprint));

            Assert.That(ex!.Message, Does.Contain("has expired"));
        }

        [Test]
        public void GetPrivateJwk_WithNotYetValidCertificate_ThrowsInvalidOperationException()
        {
            var fakeTime = new FakeTimeProvider(new DateTimeOffset(2024, 6, 1, 12, 0, 0, TimeSpan.Zero));
            var thumbprint = "NOT_YET_VALID_CERT";
            using var rsa = RSA.Create(RsaKeySize);
            using var notYetValidCert = CreateSelfSignedCertificateWithDates(
                "CN=NotYetValidCert",
                rsa,
                notBefore: new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero),
                notAfter: new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
            var provider = new FakeCertificateProvider(notYetValidCert, rsa, thumbprint);
            var sut = new PrivateKeyHandler(provider, fakeTime);

            var ex = Assert.Throws<InvalidOperationException>(() => sut.GetPrivateJwk(thumbprint));

            Assert.That(ex!.Message, Does.Contain("is not yet valid"));
        }

        [Test]
        public void GetPrivateJwk_WithValidCertificate_WhenTimeIsBeforeExpiry_ReturnsJwk()
        {
            var fakeTime = new FakeTimeProvider(new DateTimeOffset(2024, 6, 1, 12, 0, 0, TimeSpan.Zero));
            var thumbprint = "VALID_CERT";
            using var rsa = RSA.Create(RsaKeySize);
            using var validCert = CreateSelfSignedCertificateWithDates(
                "CN=ValidCert",
                rsa,
                notBefore: new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero),
                notAfter: new DateTimeOffset(2025, 12, 31, 23, 59, 59, TimeSpan.Zero));
            var provider = new FakeCertificateProvider(validCert, rsa, thumbprint);
            var sut = new PrivateKeyHandler(provider, fakeTime);

            var jwkString = sut.GetPrivateJwk(thumbprint);

            AssertValidJwk(jwkString);
        }

        [Test]
        public void GetPrivateJwk_Scenario_ValidCertificateExpiresOverTime_ThrowsOnSubsequentCall()
        {
            var fakeTime = new FakeTimeProvider(new DateTimeOffset(2024, 6, 1, 12, 0, 0, TimeSpan.Zero));
            var thumbprint = "WILL_EXPIRE";
            using var rsa = RSA.Create(RsaKeySize);
            using var cert = CreateSelfSignedCertificateWithDates(
                "CN=WillExpire",
                rsa,
                notBefore: new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero),
                notAfter: new DateTimeOffset(2024, 12, 31, 23, 59, 59, TimeSpan.Zero));
            var provider = new FakeCertificateProvider(cert, rsa, thumbprint);
            var sut = new PrivateKeyHandler(provider, fakeTime);

            var jwkString = sut.GetPrivateJwk(thumbprint);
            AssertValidJwk(jwkString);

            fakeTime.Advance(TimeSpan.FromDays(365));

            var ex = Assert.Throws<InvalidOperationException>(() => sut.GetPrivateJwk(thumbprint));
            Assert.That(ex!.Message, Does.Contain("has expired"));
        }

        [TestCase("SGVsbG8gV29ybGQ=", true, Description = "Valid Base64 with padding")]
        [TestCase("SGVsbG8gV29ybGQh", true, Description = "Valid Base64 without padding")]
        [TestCase("YWJjZA==", true, Description = "Valid Base64 with double padding")]
        [TestCase("YWJj", true, Description = "Valid Base64 no padding needed")]
        [TestCase("!!!invalid!!!", false, Description = "Invalid characters")]
        [TestCase("abc", false, Description = "Too short (not multiple of 4 - but still valid format)")]
        [TestCase("", false, Description = "Empty string")]
        [TestCase("   ", false, Description = "Whitespace only")]
        [TestCase("SGVs bG8=", false, Description = "Contains space")]
        public void IsBase64String_ReturnsExpectedResult(string input, bool expectedResult)
        {
            // We need to test the private method indirectly through GetPrivateJwk behavior
            // For this test, we'll use reflection to access the private method
            var method = typeof(PrivateKeyHandler).GetMethod("IsBase64String",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            Assert.That(method, Is.Not.Null, "IsBase64String method should exist");

            var result = (bool)method!.Invoke(null, [input])!;
            Assert.That(result, Is.EqualTo(expectedResult), $"Input: '{input}'");
        }

        [Test]
        public void GetPrivateJwk_WithValidBase64ButInvalidJson_CatchesArgumentExceptionAndFallsBack()
        {
            var notJsonText = "This is not JSON at all, just plain text that is long enough to exceed 100 chars when base64 encoded";
            var invalidJsonBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(notJsonText));
            Assert.That(invalidJsonBase64.Length, Is.GreaterThan(100), "Test setup: Base64 should be > 100 chars");
            var provider = new FakeCertificateProvider(null, null, null);
            var sut = new PrivateKeyHandler(provider);

            var ex = Assert.Throws<InvalidOperationException>(() => sut.GetPrivateJwk(invalidJsonBase64));

            Assert.That(ex!.Message, Does.Contain("No certificate found"));
        }

        [Test]
        public void GetPrivateJwk_WithValidBase64ValidJson_ReturnsAsJwk()
        {
            var validJson = "{\"name\": \"test\", \"value\": 12345, \"nested\": {\"foo\": \"bar\", \"description\": \"This is a longer string to ensure the Base64 encoding exceeds 100 characters\"}}";
            var validBase64ValidJson = Convert.ToBase64String(Encoding.UTF8.GetBytes(validJson));
            Assert.That(validBase64ValidJson.Length, Is.GreaterThan(100), "Test setup: Base64 should be > 100 chars");
            var provider = new FakeCertificateProvider(null, null, null);
            var sut = new PrivateKeyHandler(provider);

            var result = sut.GetPrivateJwk(validBase64ValidJson);

            Assert.That(result, Is.EqualTo(validJson));
        }

        private static X509Certificate2 CreateSelfSignedCertificate(string subject, RSA rsa)
        {
            var request = new CertificateRequest(
                subject,
                rsa,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);

            // Use fixed dates for deterministic tests
            var baseDate = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
            return request.CreateSelfSigned(baseDate, baseDate.AddYears(2));
        }

        private static X509Certificate2 CreateSelfSignedCertificateWithDates(
            string subject,
            RSA rsa,
            DateTimeOffset notBefore,
            DateTimeOffset notAfter)
        {
            var request = new CertificateRequest(
                subject,
                rsa,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);

            return request.CreateSelfSigned(notBefore, notAfter);
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

        private class FakeCertificateProvider : ICertificateProvider
        {
            private readonly string? _thumb;
            private readonly RSA? _rsa;
            private readonly DateTimeOffset _notBefore;
            private readonly DateTimeOffset _notAfter;
            private readonly string _subject;
            private readonly byte[]? _publicOnlyCertBytes;

            public FakeCertificateProvider(X509Certificate2? cert, RSA? rsa, string? thumb)
            {
                _thumb = thumb;
                _rsa = rsa;
                _subject = cert?.Subject ?? "CN=Test";
                // Use fixed dates for deterministic tests
                var defaultBaseDate = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
                _notBefore = cert?.NotBefore ?? defaultBaseDate;
                _notAfter = cert?.NotAfter ?? defaultBaseDate.AddYears(2);

                // Store bytes for public-only certificates (no private key)
                if (cert != null && rsa == null)
                {
                    _publicOnlyCertBytes = cert.Export(X509ContentType.Cert);
                }
            }

            public X509Certificate2? GetCertificate(string thumbprint)
            {
                if (!MatchesThumbprint(thumbprint))
                    return null;

                // Case 1: Public-only certificate - recreate from stored bytes
                if (_publicOnlyCertBytes != null)
                {
#if NET9_0_OR_GREATER
                    return X509CertificateLoader.LoadCertificate(_publicOnlyCertBytes);
#else
                    return new X509Certificate2(_publicOnlyCertBytes);
#endif
                }

                // Case 2: Certificate with private key - create fresh self-signed cert
                if (_rsa != null)
                {
                    var request = new CertificateRequest(_subject, _rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                    return request.CreateSelfSigned(_notBefore, _notAfter);
                }

                return null;
            }

            public RSA? GetPrivateKey(string thumbprint) =>
                MatchesThumbprint(thumbprint) ? _rsa : null;

            private bool MatchesThumbprint(string thumbprint) =>
                _thumb != null && string.Equals(thumbprint, _thumb, StringComparison.OrdinalIgnoreCase);
        }
    }
}
