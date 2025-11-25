using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Fhi.Authentication.Tokens
{
    /// <summary>
    /// Abstraction to obtain certificates and their private keys.
    /// Implementations may read from the OS certificate store or provide in-memory certificates for tests.
    /// </summary>
    public interface ICertificateProvider
    {
        /// <summary>
        /// Returns the X509Certificate2 for the provided normalized thumbprint from the CurrentUser\My store, or null if not found.
        /// </summary>
        X509Certificate2? GetCertificate(string normalizedThumbprint);

        /// <summary>
        /// Returns an RSA representing the private key for the certificate, or null if none is present.
        /// Caller is responsible for disposing the RSA when appropriate.
        /// </summary>
        RSA? GetPrivateKey(string normalizedThumbprint);
    }
    
    /// <summary>
    /// Default certificate provider that resolves certificates from CurrentUser\My.
    /// </summary>
    public class StoreCertificateProvider : ICertificateProvider
    {
        /// <summary>
        /// Returns the X509Certificate2 for the provided normalized thumbprint from the CurrentUser\My store, or null if not found.
        /// </summary>
        /// <param name="normalizedThumbprint">Thumbprint normalized (uppercase, no spaces).</param>
        public X509Certificate2? GetCertificate(string normalizedThumbprint)
        {
            if (string.IsNullOrWhiteSpace(normalizedThumbprint)) throw new ArgumentNullException(nameof(normalizedThumbprint));

            using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadOnly);

            var matches = store.Certificates.Find(X509FindType.FindByThumbprint, normalizedThumbprint, validOnly: false);
            if (matches.Count == 0) return null;

            return matches[0];
        }

        /// <summary>
        /// Returns the RSA private key for the certificate with the specified normalized thumbprint, or null if no private key is available.
        /// </summary>
        /// <param name="normalizedThumbprint">Thumbprint normalized (uppercase, no spaces).</param>
        /// <returns>An <see cref="RSA"/> representing the private key, or null.</returns>
        public RSA? GetPrivateKey(string normalizedThumbprint)
        {
            var cert = GetCertificate(normalizedThumbprint);
            return cert?.GetRSAPrivateKey();
        }
    }
}