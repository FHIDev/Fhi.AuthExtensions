using System.ComponentModel.DataAnnotations;
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
        /// Client assertion issuer value
        /// </summary>
        public string Issuer { get; set; } = string.Empty;

        /// <summary>
        /// The client assertion type
        /// </summary>
        public string ClientAssertionType { get; set; } = ClientAssertionTypes.JwtBearer;

        /// <summary>
        /// The client assertion expiration time in seconds. Default is 10 seconds.
        /// Must be greater than or equal to 0.
        /// </summary>
        [Range(0, int.MaxValue, ErrorMessage = "ExpirationSeconds must be greater than or equal to 0.")]
        public int ExpirationSeconds { get; set; } = 10;
    }
}
