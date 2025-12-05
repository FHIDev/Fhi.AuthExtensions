using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using Microsoft.IdentityModel.Tokens;

namespace Fhi.Authentication.Tokens
{
    /// <summary>
    /// Abstraction for resolving a certificate private key as a JWK.
    /// </summary>
    public interface ICertificateKeyHandler
    {
        /// <summary>
        /// Returns the private key for the certificate identified by <paramref name="certificateThumbprint"/> serialized as a JWK.
        /// </summary>
        /// <param name="certificateThumbprint">Certificate thumbprint (not null or empty).</param>
        /// <returns>Serialized JWK string.</returns>
        public string GetPrivateKeyAsJwk(string certificateThumbprint);
    }

    /// <summary>
    /// Resolves an X509 certificate (via an injected provider) and returns its private key as a JWK.
    /// </summary>
    public class CertificateKeyHandler : ICertificateKeyHandler
    {
        private readonly ICertificateProvider _certificateProvider;

        /// <summary>
        /// Constructor for DI/test injection.
        /// </summary>
        public CertificateKeyHandler(ICertificateProvider certificateProvider)
        {
            _certificateProvider = certificateProvider ?? throw new ArgumentNullException(nameof(certificateProvider));
        }

        /// <inheritdoc/>
        public string GetPrivateKeyAsJwk(string certificateThumbprint)
        {
            if (string.IsNullOrWhiteSpace(certificateThumbprint))
            {
                throw new ArgumentNullException(nameof(certificateThumbprint));
            }

            var normalizedThumbprint = NormalizeThumbprint(certificateThumbprint);

            // Get the certificate and ensure it's disposed after use
            using var certificate = _certificateProvider.GetCertificate(normalizedThumbprint);
            if (certificate == null)
            {
                throw new InvalidOperationException($"Certificate not found for thumbprint: {normalizedThumbprint}");
            }

            ValidateCertificate(certificate);
            
            // Capture the thumbprint before the certificate is disposed
            var keyId = certificate.Thumbprint;
            
            // Extract the private key from the certificate
            // The RSA key is independent of the certificate and remains valid after cert disposal
            using var rsa = certificate.GetRSAPrivateKey();
            if (rsa == null)
            {
                throw new InvalidOperationException($"Certificate {certificate.Subject} has no private key available");
            }

            var rsaParameters = rsa.ExportParameters(true);
            var jwk = new RsaSecurityKey(rsaParameters)
            {
                KeyId = keyId
            };

            var jsonWebKey = JsonWebKeyConverter.ConvertFromRSASecurityKey(jwk);
            return JsonSerializer.Serialize(jsonWebKey);
        }

        private static string NormalizeThumbprint(string thumbprint) =>
            thumbprint.Replace(" ", string.Empty).ToUpperInvariant();

        private static void ValidateCertificate(X509Certificate2 certificate)
        {
            if (certificate is null) throw new ArgumentNullException(nameof(certificate));

            if (certificate.NotAfter < DateTime.UtcNow)
            {
                throw new InvalidOperationException($"Certificate {certificate.Subject} has expired on {certificate.NotAfter}");
            }
        }
    }
}