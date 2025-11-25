using static Duende.IdentityModel.OidcConstants;

namespace Fhi.Authentication.ClientCredentials
{
    /// <summary>
    /// Used to create client assertion parameters for a specific client
    /// </summary>
    public class ClientAssertionOptions
    {
        /// <summary>
        /// Client assertion private JWK
        /// </summary>
        public string PrivateJwk { get; set; } = string.Empty;
        
        /// <summary>
        /// Thumbprint of PEM certificate
        /// </summary>
        public string CertificateThumbprint { get; set; } = string.Empty;

        /// <summary>
        /// Client assertion issuer value
        /// </summary>
        public string Issuer { get; set; } = string.Empty;

        /// <summary>
        /// The client assertion type
        /// </summary>
        public string ClientAssertionType { get; set; } = ClientAssertionTypes.JwtBearer;
    }
}
