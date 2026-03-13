namespace Fhi.Authentication.JwtDPoP
{
    internal static partial class DPoPConstants
    {
        public const string Scheme = "DPoP";
        public const string DPoPHeaderName = "DPoP";



        // JWT claim types
        public const string Confirmation = "cnf";
        public const string ConfirmationMethodJwkThumbprint = "jkt";
        public const string JsonWebKey = "jwk";
        public const string JwtId = "jti";
        public const string IssuedAt = "iat";
        public const string DPoPAccessTokenHash = "ath";
        public const string DPoPHttpMethod = "htm";
        public const string DPoPHttpUrl = "htu";
        public const string Nonce = "nonce";

        // Token types
        public const string DPoPProofTokenType = "dpop+jwt";

        // Error codes
        public const string InvalidToken = "invalid_token";
        public const string InvalidDPoPProof = "invalid_dpop_proof";
        public const string InvalidRequest = "invalid_request";
        ////public const string UseDPoPNonce = "use_dpop_nonce";
    }

    internal static partial class DPoPConstants
    {
        internal static class ItemPropertyName
        {
            public const string DPoPFailureCode = "dpop_failure_error";
            public const string DPoPFailureDescription = "dpop_failure_description";
        }
    }
}
