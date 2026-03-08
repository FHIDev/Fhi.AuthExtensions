namespace Fhi.Authentication.JwtDPoP
{
    /// <summary>
    /// Specifies the modes used to validate expiration times in security tokens.
    /// </summary>
    /// <remarks>Use this enumeration to select the strategy for validating the expiration of security tokens,
    /// such as whether to rely on a server-issued nonce, the 'iat' (issued at) claim in the proof token, or both. The
    /// chosen mode affects how token freshness and replay protection are enforced during authentication.</remarks>
    public enum ProofLifetimeValidationType
    {
        /// <summary>
        /// Validate the time from the iat claim in the proof token
        /// </summary>
        IssuedAt
    }
}
