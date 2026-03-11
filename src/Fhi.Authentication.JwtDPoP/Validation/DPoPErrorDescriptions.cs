namespace Fhi.Authentication.JwtDPoP.Validation
{
    internal static class DPoPErrorDescriptions
    {
        public const string MissingHeader = "DPoP proof header missing";
        public const string MultipleHeaders = "Multiple DPoP proof headers";
        public const string MalformedJwt = "DPoP proof is malformed";
        public const string InvalidTyp = "typ header must be dpop+jwt";
        public const string DisallowedAlg = "Disallowed algorithm";
        public const string PrivateKeyInJwk = "jwk header must not contain private key material";
        public const string InvalidSignature = "Invalid DPoP proof signature.";
        public const string MissingRequiredClaim = "Missing required claim";
        public const string MissingRequiredClaimHtm = "Missing required claim htm";
        public const string MissingRequiredClaimHtu = "Missing required claim htu";
        public const string MissingRequiredClaimAth = "Missing required claim ath";
        public const string MissingRequiredClaimJti = "Missing required claim jti";
        public const string MissingRequiredClaimIat = "Missing required claim iat";
        public const string HtmMismatch = "htm claim does not match the request method";
        public const string HtuMismatch = "htu claim does not match the request URI";
        public const string IatTooOld = "iat claim is expired";
        public const string IatTooFarInFuture = "iat claim is too far in the future";
        public const string AthMismatch = "ath claim does not match the access token hash";
        public const string KeyBindingMismatch = "invalid token binding";
        public const string JtiTooLong = "jti claim exceeds maximum allowed length";
        public const string JtiReplay = "Detected DPoP proof token replay";
    }
}
