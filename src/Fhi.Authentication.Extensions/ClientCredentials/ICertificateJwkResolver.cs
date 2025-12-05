using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using Fhi.Authentication.Tokens;
using Microsoft.IdentityModel.Tokens;

namespace Fhi.Authentication.ClientCredentials
{
    /// <summary>
    /// Service responsible for converting certificate configurations to JWK format.
    /// </summary>
    public interface ICertificateJwkResolver
    {
        /// <summary>
        /// Resolves a certificate configuration to a JWK JSON string.
        /// Supports both certificate store (via thumbprint) and PEM-encoded certificates.
        /// </summary>
        /// <param name="options">The certificate options containing either a thumbprint or PEM certificate.</param>
        /// <returns>A JWK JSON string representing the certificate's private key.</returns>
        /// <exception cref="ArgumentNullException">When options is null.</exception>
        /// <exception cref="InvalidOperationException">When neither valid thumbprint nor PEM certificate is provided, or certificate has no private key.</exception>
        string ResolveToJwk(CertificateOptions options);
    }

    /// <summary>
    /// Default implementation of certificate-to-JWK resolver.
    /// Handles both certificate store (via thumbprint) and PEM-encoded certificates.
    /// </summary>
    public class CertificateJwkResolver : ICertificateJwkResolver
    {
        /// <summary>
        /// Resolves a certificate configuration to a JWK JSON string.
        /// </summary>
        public string ResolveToJwk(CertificateOptions options)
        {
            using var certificate = GetCertificate(options);
            return GetJwkFromCertificate(certificate);
        }

        /// <summary>
        /// Retrieves a certificate from the provided options.
        /// </summary>
        private X509Certificate2 GetCertificate(CertificateOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            // Priority 1: Check thumbprint from certificate store
            if (!string.IsNullOrWhiteSpace(options.Thumbprint))
            {
                var certProvider = new StoreCertificateProvider(options.StoreLocation);
                var cert = certProvider.GetCertificate(options.Thumbprint);
                if (cert == null)
                {
                    throw new InvalidOperationException(
                        $"Certificate not found for thumbprint: {options.Thumbprint}");
                }
                return cert;
            }

            // Priority 2: Fall back to PEM certificate if it looks like actual PEM content
            if (IsPemContent(options.PemCertificate))
            {
                return X509Certificate2.CreateFromPem(options.PemCertificate!);
            }

            throw new InvalidOperationException(
                "Either a valid Thumbprint or PemCertificate (starting with -----BEGIN) must be provided in CertificateOptions.");
        }

        /// <summary>
        /// Converts a certificate to a JWK JSON string.
        /// </summary>
        private string GetJwkFromCertificate(X509Certificate2 certificate)
        {
            if (certificate == null)
                throw new ArgumentNullException(nameof(certificate));

            using var rsa = certificate.GetRSAPrivateKey();
            if (rsa == null)
            {
                throw new InvalidOperationException(
                    $"Certificate {certificate.Subject} has no private key available");
            }

            var rsaParameters = rsa.ExportParameters(true);
            var jwk = new RsaSecurityKey(rsaParameters)
            {
                KeyId = certificate.Thumbprint
            };

            var jsonWebKey = JsonWebKeyConverter.ConvertFromRSASecurityKey(jwk);
            return JsonSerializer.Serialize(jsonWebKey);
        }


        private static bool IsPemContent(string? pem) =>
            !string.IsNullOrWhiteSpace(pem) && pem.TrimStart().StartsWith("-----BEGIN");
    }
}