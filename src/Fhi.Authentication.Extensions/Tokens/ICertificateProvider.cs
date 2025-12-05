using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Fhi.Authentication.Tokens
{
    /// <summary>
    /// Specifies the certificate store location.
    /// </summary>
    public enum CertificateStoreLocation
    {
        /// <summary>
        /// The certificate store for the current user (CurrentUser\My).
        /// </summary>
        CurrentUser,

        /// <summary>
        /// The certificate store for the local machine (LocalMachine\My).
        /// </summary>
        LocalMachine
    }

    /// <summary>
    /// Abstraction to obtain certificates and their private keys.
    /// Implementations may read from the OS certificate store or provide in-memory certificates for tests.
    /// </summary>
    public interface ICertificateProvider
    {
        /// <summary>
        /// Returns the X509Certificate2 for the provided normalized thumbprint, or null if not found.
        /// </summary>
        /// <param name="normalizedThumbprint">Thumbprint normalized (uppercase, no spaces).</param>
        /// <returns>An X509Certificate2 instance that must be disposed by the caller, or null if not found.</returns>
        /// <remarks>
        /// Caller is responsible for disposing the returned certificate to prevent memory leaks.
        /// </remarks>
        X509Certificate2? GetCertificate(string normalizedThumbprint);

        /// <summary>
        /// Returns an RSA representing the private key for the certificate, or null if none is present.
        /// </summary>
        /// <param name="normalizedThumbprint">Thumbprint normalized (uppercase, no spaces).</param>
        /// <returns>An RSA instance that must be disposed by the caller, or null if no private key is available.</returns>
        /// <remarks>
        /// Caller is responsible for disposing the returned RSA instance to prevent memory leaks.
        /// </remarks>
        RSA? GetPrivateKey(string normalizedThumbprint);
    }
    
    /// <summary>
    /// Default certificate provider that resolves certificates from the Windows certificate store.
    /// Supports both CurrentUser\My and LocalMachine\My stores.
    /// </summary>
    public class StoreCertificateProvider : ICertificateProvider
    {
        private readonly StoreLocation _storeLocation;

        /// <summary>
        /// Initializes a new instance of <see cref="StoreCertificateProvider"/>.
        /// </summary>
        /// <param name="certificateStoreLocation">The certificate store location (CurrentUser or LocalMachine).</param>
        public StoreCertificateProvider(CertificateStoreLocation certificateStoreLocation = CertificateStoreLocation.CurrentUser)
        {
            _storeLocation = certificateStoreLocation == CertificateStoreLocation.CurrentUser
                ? StoreLocation.CurrentUser
                : StoreLocation.LocalMachine;
        }

        /// <summary>
        /// Returns the X509Certificate2 for the provided normalized thumbprint, or null if not found.
        /// </summary>
        /// <param name="normalizedThumbprint">Thumbprint normalized (uppercase, no spaces).</param>
        public X509Certificate2? GetCertificate(string normalizedThumbprint)
        {
            if (string.IsNullOrWhiteSpace(normalizedThumbprint)) 
                throw new ArgumentNullException(nameof(normalizedThumbprint));

            using var store = new X509Store(StoreName.My, _storeLocation);
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
        /// <remarks>
        /// Caller is responsible for disposing the returned RSA instance.
        /// This method handles certificate disposal internally.
        /// </remarks>
        public RSA? GetPrivateKey(string normalizedThumbprint)
        {
            var cert = GetCertificate(normalizedThumbprint);
            if (cert == null) return null;

            try
            {
                return cert.GetRSAPrivateKey();
            }
            finally
            {
                // Dispose the certificate after extracting the key
                cert.Dispose();
            }
        }
    }
}