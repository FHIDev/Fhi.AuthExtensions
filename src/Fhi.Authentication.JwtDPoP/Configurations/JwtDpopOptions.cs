using Microsoft.AspNetCore.Authentication;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace Fhi.Authentication.JwtDPoP
{
    /// <summary>
    /// Provides configuration options for DPoP (Demonstration of Proof-of-Possession) token authentication within an
    /// ASP.NET Core application.
    /// </summary>
    /// <remarks>Use this class to customize the behavior of DPoP token authentication, including specifying
    /// the authority, audience, and token validation parameters. The options allow control over metadata retrieval,
    /// HTTPS requirements, and whether to persist the validated token. These settings are typically supplied when
    /// adding DPoP authentication to the authentication middleware pipeline.</remarks>
    public class JwtDpopOptions : AuthenticationSchemeOptions
    {
        /// <summary>
        /// Gets or sets the authority that represents the security or identity provider associated with the current
        /// context.
        /// </summary>
        /// <remarks>This property can be null, indicating that no authority is specified. Ensure that the
        /// authority is set appropriately to maintain the integrity of authentication and authorization
        /// operations.</remarks>
        public string? Authority { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string? Audience { get; set; }
        /// <summary>
        /// Gets or sets the address of the metadata associated with the entity.
        /// </summary>
        /// <remarks>This property can be null if no metadata address is specified. Ensure that the
        /// address is a valid URI format when setting this property.</remarks>
        public string? MetadataAddress { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public bool RequireHttpsMetadata { get; set; } = true;
        /// <summary>
        /// 
        /// </summary>
        public bool SaveToken { get; set; } = true;
        /// <summary>
        /// Gets or sets the parameters used to validate security tokens during authentication.
        /// </summary>
        /// <remarks>Use this property to configure how tokens are validated, including issuer, audience,
        /// lifetime, and signature requirements. Proper configuration is essential to ensure secure token validation
        /// and prevent unauthorized access.</remarks>
        public TokenValidationParameters TokenValidationParameters { get; set; } = new TokenValidationParameters
        {
            ValidateAudience = true,
            ValidateIssuer = true,
            ValidateLifetime = true
        };

        /// <summary>
        /// Gets or sets the parameters used to validate DPoP proof tokens.
        /// </summary>
        /// <remarks>Configure this property to specify the validation requirements for DPoP proof tokens,
        /// such as accepted algorithms, clock skew, or other constraints. Proper configuration is essential to ensure
        /// the integrity and authenticity of DPoP tokens during authentication or authorization processes.</remarks>
        public DPoPProofTokenValidationParameters DPoPProofTokenValidationParameters { get; set; } = new DPoPProofTokenValidationParameters();

        /// <summary>
        /// Gets or sets the OpenID Connect configuration used for authentication.
        /// </summary>
        /// <remarks>This property holds the configuration details necessary for establishing an OpenID
        /// Connect authentication flow. Ensure that the configuration is properly set before initiating authentication
        /// requests.</remarks>
        public OpenIdConnectConfiguration? Configuration { get; set; }
    }
}
