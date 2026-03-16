using System.Security.Cryptography.X509Certificates;

namespace Fhi.Authentication.Certificate
{
    /// <summary>
    /// 
    /// </summary>
    internal interface ICertificateProvider
    {
        /// <summary>
        /// Returns the X509Certificate2 for the provided normalized thumbprint, or null if not found.
        /// </summary>
        /// <param name="thumbprint">Thumbprint normalized (uppercase, no spaces).</param>
        /// <param name="storeName"></param>
        /// <param name="storeLocation"></param>
        /// <returns>An X509Certificate2 instance that must be disposed by the caller, or null if not found.</returns>
        /// <remarks>
        /// Caller is responsible for disposing the returned certificate to prevent memory leaks.
        /// </remarks>
        X509Certificate2? GetCertificate(string thumbprint, StoreName storeName = StoreName.My, StoreLocation storeLocation = StoreLocation.CurrentUser);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="certificate"></param>
        /// <returns></returns>
        CertificateValidationResult Validate(X509Certificate2 certificate);
    }

    /// <summary>
    /// Default certificate provider that resolves certificates from the Windows certificate store.
    /// Supports both CurrentUser\My and LocalMachine\My stores.
    /// </summary>
    public class DefaultStoreCertificateProvider(TimeProvider timeProvider) : ICertificateProvider
    {
        /// <summary>
        /// Returns the X509Certificate2 for the provided normalized thumbprint, or null if not found.
        /// </summary>
        /// <param name="thumbprint">Thumbprint normalized (uppercase, no spaces).</param>
        /// <param name="storeName"></param>
        /// <param name="storeLocation"></param>
        public X509Certificate2? GetCertificate(string thumbprint, StoreName storeName = StoreName.My, StoreLocation storeLocation = StoreLocation.CurrentUser)
        {
            using var store = new X509Store(storeName, storeLocation);
            store.Open(OpenFlags.ReadOnly);
            var matches = store.Certificates.Find(X509FindType.FindByThumbprint, thumbprint, validOnly: false);
            if (matches.Count == 0) return null;
            store.Close();

            return matches[0];
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="certificate"></param>
        /// <returns></returns>
        public CertificateValidationResult Validate(X509Certificate2 certificate)
        {
            var utcNow = timeProvider.GetUtcNow().UtcDateTime;
            if (certificate.NotAfter.ToUniversalTime() < utcNow)
            {
                return new CertificateValidationResult(false, "");
            }

            if (certificate.NotBefore.ToUniversalTime() > utcNow)
            {
                return new CertificateValidationResult(false, "");
            }

            return new CertificateValidationResult(true);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="Success"></param>
    /// <param name="ErrorDescription"></param>
    public record CertificateValidationResult(bool Success, string? ErrorDescription = null);
}