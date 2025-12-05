using System.ComponentModel.DataAnnotations;
using Fhi.Authentication.ClientCredentials;

namespace M2M.Host.HelseID
{
    /// <summary>
    /// Configuration for HelseID API with certificate-based authentication.
    /// Alternative to HelseIdProtectedApiOption that uses certificate instead of inline JWK.
    /// </summary>
    public class HelseIdCertificateApiOption
    {
        [Required] public string BaseAddress { get; set; } = string.Empty;
        [Required] public HelseIDCertificateAuthentication Authentication { get; set; } = new();
        public string ClientName => "HelseIdProtectedApi";
    }

    public class HelseIDCertificateAuthentication
    {
        [Required] public string Authority { get; set; } = string.Empty;
        [Required] public string ClientId { get; set; } = string.Empty;
        [Required] public string Scope { get; set; } = string.Empty;
        
        /// <summary>
        /// Certificate configuration - will be converted to JWK by ICertificateJwkResolver
        /// </summary>
        [Required] public CertificateOptions Certificate { get; set; } = new();
    }
}