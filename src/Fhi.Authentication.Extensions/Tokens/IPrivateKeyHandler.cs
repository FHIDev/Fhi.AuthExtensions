using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.DependencyInjection;

namespace Fhi.Authentication.Tokens
{
    /// <summary>
    /// Abstraction for resolving a private key as a JWK from multiple input formats.
    /// Handles secrets from any source (files, Azure Key Vault, certificate stores, etc.) in various formats.
    /// </summary>
    public interface IPrivateKeyHandler
    {
        /// <summary>
        /// Resolves a private key JWK (as JSON string) from multiple supported formats.
        /// Automatically detects the input format and converts to JWK.
        /// </summary>
        /// <param name="secretOrThumbprint">The secret in one of the following formats:
        /// <list type="bullet">
        /// <item><description>JWK JSON string (starts with "{")</description></item>
        /// <item><description>PEM-encoded private key (starts with "-----BEGIN")</description></item>
        /// <item><description>Base64-encoded JWK string</description></item>
        /// <item><description>Certificate thumbprint (fetches from Windows certificate store)</description></item>
        /// </list>
        /// </param>
        /// <returns>Private key as JWK JSON string.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="secretOrThumbprint"/> is null or empty.</exception>
        /// <exception cref="ArgumentException">Thrown if the input format is invalid.</exception>
        /// <exception cref="InvalidOperationException">Thrown if certificate is not found, expired, or has no private key.</exception>
        string GetPrivateJwk(string secretOrThumbprint);
    }

    /// <summary>
    /// Default implementation of <see cref="IPrivateKeyHandler"/> that handles private keys from multiple sources and formats.
    /// Supports certificate stores, direct PEM/JWK input, and Base64-encoded secrets.
    /// </summary>
    public class PrivateKeyHandler : IPrivateKeyHandler
    {
        private readonly ICertificateProvider _certificateProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="PrivateKeyHandler"/> class.
        /// </summary>
        /// <param name="certificateProvider">Provider for accessing certificates from certificate stores.</param>
        public PrivateKeyHandler(ICertificateProvider certificateProvider)
        {
            _certificateProvider = certificateProvider ?? throw new ArgumentNullException(nameof(certificateProvider));
        }

        /// <inheritdoc/>
        public string GetPrivateJwk(string secretOrThumbprint)
        {
            if (string.IsNullOrWhiteSpace(secretOrThumbprint))
            {
                throw new ArgumentNullException(nameof(secretOrThumbprint));
            }

            var trimmed = secretOrThumbprint.TrimStart();
            
            // Format Detection: Check input format and route to appropriate parser
            
            // 1. Check if it's already JWK JSON format
            if (trimmed.StartsWith("{", StringComparison.Ordinal))
            {
                // Direct JWK input - validate and return
                var privateJwk = PrivateJwk.ParseFromJson(secretOrThumbprint);
                return privateJwk; 
            }

            // 2. Check if it's PEM format
            if (trimmed.StartsWith("-----BEGIN", StringComparison.OrdinalIgnoreCase))
            {
                // Direct PEM input - parse it to JWK
                var privateJwk = PrivateJwk.ParseFromPem(secretOrThumbprint);
                return privateJwk; // implicit conversion to string
            }

            // 3. Check if it's Base64-encoded JWK (heuristic: length > 100 and valid base64)
            if (IsBase64String(trimmed) && trimmed.Length > 100)
            {
                try
                {
                    var privateJwk = PrivateJwk.ParseFromBase64Encoded(secretOrThumbprint);
                    return privateJwk; // implicit conversion to string
                }
                catch
                {
                    // Not valid Base64 JWK, fall through to thumbprint handling
                }
            }
            
            // 4. Default: Treat as certificate thumbprint - fetch certificate, get PEM, convert to JWK
            var pem = GetPrivateKeyAsPemFromThumbprint(secretOrThumbprint);
            var jwk = PrivateJwk.ParseFromPem(pem);
            return jwk; // implicit conversion to string
        }
        
        private static bool IsBase64String(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;
            
            value = value.Trim();
            return (value.Length % 4 == 0) && 
                   System.Text.RegularExpressions.Regex.IsMatch(value, @"^[a-zA-Z0-9\+/]*={0,2}$");
        }

        /// <summary>
        /// Internal helper to extract PEM from certificate thumbprint.
        /// </summary>
        private string GetPrivateKeyAsPemFromThumbprint(string certificateThumbprint)
        {
            var normalizedThumbprint = NormalizeThumbprint(certificateThumbprint);

            // Get the certificate and ensure it's disposed after use
            using var certificate = _certificateProvider.GetCertificate(normalizedThumbprint);
            if (certificate == null)
            {
                throw new InvalidOperationException($"No certificate found for thumbprint: {normalizedThumbprint}. Make sure the certificate is installed in CurrentUser\\My store");
            }

            ValidateCertificate(certificate);
            
            using var rsa = certificate.GetRSAPrivateKey();
            if (rsa == null)
            {
                throw new InvalidOperationException($"Certificate {certificate.Subject} has no private key available");
            }
            
            return rsa.ExportRSAPrivateKeyPem();
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