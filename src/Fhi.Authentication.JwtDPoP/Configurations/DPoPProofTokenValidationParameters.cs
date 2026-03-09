using Microsoft.IdentityModel.Tokens;

namespace Fhi.Authentication.JwtDPoP
{
    /// <summary>
    /// Provides token validation parameters specifically configured for validating DPoP proof tokens during
    /// authentication processes.
    /// </summary>
    /// <remarks>This class inherits from TokenValidationParameters and is preconfigured to accept DPoP proof
    /// tokens by setting the valid token types accordingly. By default, audience, issuer, and lifetime validations are
    /// disabled, allowing for more flexible validation scenarios where these checks are not required. Use this class
    /// when validating DPoP proof tokens in scenarios where standard JWT validation parameters are too
    /// restrictive.</remarks>
    public class DPoPProofTokenValidationParameters
    {
        /// <summary>
        /// Gets or sets the maximum allowed length, in characters, for a proof token.
        /// </summary>
        /// <remarks>Set this property to restrict the size of proof tokens that can be processed. Ensure
        /// that the specified value does not exceed the limitations of any underlying storage or processing
        /// components.</remarks>
        public int ProofTokenMaxLength { get; set; } = 4000;

        /// <summary>
        /// Gets or sets the allowable clock skew between the client and server when validating time-sensitive tokens.
        /// </summary>
        /// <remarks>This property accounts for minor differences in system clocks, helping to prevent
        /// authentication failures due to small time discrepancies. Adjust this value if clients and servers are known
        /// to have significant clock drift.</remarks>
        public TimeSpan AllowedClockSkew { get; set; } = TimeSpan.FromMinutes(5);


        /// <summary>
        /// DPoP token Algoritmepolicy (JOSE)
        /// </summary>
        public IEnumerable<string> ValidAlgorithms { get; set; } = new[]
                    {
                        SecurityAlgorithms.RsaSha256,
                        SecurityAlgorithms.RsaSha384,
                        SecurityAlgorithms.RsaSha512,
                        SecurityAlgorithms.RsaSsaPssSha256,
                        SecurityAlgorithms.EcdsaSha256,
                        SecurityAlgorithms.EcdsaSha384,
                        SecurityAlgorithms.EcdsaSha512
                    };

        /// <summary>
        /// Gets or sets the duration for which a proof token remains valid.
        /// </summary>
        /// <remarks>The default value is 60 seconds. Adjust this duration to meet the application's
        /// security requirements. Shorter durations increase security but may require more frequent token
        /// renewals.</remarks>
        public TimeSpan ProofTokenLifetimeValidationDuration { get; set; } = TimeSpan.FromSeconds(60);

        ///// <summary>
        ///// TODO: later when implementing Nonce
        ///// </summary>
        ////public ProofLifetimeValidationType ProofTokenLifetimeValidationType { get; set; } = ProofLifetimeValidationType.IssuedAt;

        /// <summary>
        /// Gets or sets the maximum allowed length, in characters, for the JSON Web Token Identifier (JTI) value.
        /// </summary>
        /// <remarks>This property defines the upper limit for the number of characters permitted in the
        /// JTI claim of a JSON Web Token. When generating or validating tokens, ensure that the JTI does not exceed
        /// this length to maintain compliance and prevent errors.</remarks>
        public int MaxJtiLength { get; set; } = 200;
    }
}
