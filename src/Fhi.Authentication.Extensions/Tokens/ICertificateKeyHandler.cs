using Fhi.Authentication.ClientCredentials;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;

namespace Fhi.Authentication.Tokens
{
    /// <summary>
    /// Abstraction for resolving a certificate private key as a JWK.
    /// </summary>
    public interface ICertificateKeyHandler<T>
    {
        /// <summary>
        /// Returns the private key for the certificate identified by <paramref name="options"/> serialized as a JWK.
        /// </summary>
        /// <param name="options">Certificate thumbprint (not null or empty).</param>
        /// <returns>Serialized JWK string.</returns>
        public string GetPrivateKeyAsJwk(IJwkOptions options);
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
        public string GetPrivateKeyAsJwk(IJwkOptions certificateThumbprint)
        {
            var options = certificateThumbprint as CertificateOptions;

            if (string.IsNullOrWhiteSpace(certificateThumbprint))
            {
                throw new ArgumentNullException(nameof(certificateThumbprint));
            }

            var normalizedThumbprint = NormalizeThumbprint(certificateThumbprint);

            var certificate = _certificateProvider.GetCertificate(normalizedThumbprint);
            if (certificate == null)
            {
                throw new InvalidOperationException($"No certificate found for thumbprint: {normalizedThumbprint}. Make sure the certificate is installed in CurrentUser\\My store");
            }

            ValidateCertificate(certificate);

            using var rsa = _certificateProvider.GetPrivateKey(normalizedThumbprint);
            if (rsa == null)
            {
                throw new InvalidOperationException($"Certificate {certificate.Subject} has no private key available");
            }

            var rsaParameters = rsa.ExportParameters(true);
            var jwk = new RsaSecurityKey(rsaParameters)
            {
                KeyId = certificate.Thumbprint
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