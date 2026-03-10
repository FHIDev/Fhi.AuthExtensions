using Fhi.Authentication.JwtDPoP.Validation.Models;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Fhi.Authentication.JwtDPoP.Validation.DPoPProofValidators
{
    internal class JoseHeaderJwkValidator : IDPoPProofValidator
    {
        public Task<DPoPValidationResult> ExecuteAsync(DPoPValidationContext context, JsonWebToken proofToken, CancellationToken cancellationToken = default)
        {
            if (proofToken.SigningKey == null)
                return Task.FromResult(new DPoPValidationResult(true, DPoPConstants.InvalidDPoPProof, DPoPErrorDescriptions.MalformedJwt));

            if (proofToken.SigningKey is JsonWebKey jwk && ContainsPrivateKeyMaterial(jwk))
                return Task.FromResult(new DPoPValidationResult(true, DPoPConstants.InvalidDPoPProof, DPoPErrorDescriptions.PrivateKeyInJwk));

            return Task.FromResult(new DPoPValidationResult(false));
        }

        private static bool ContainsPrivateKeyMaterial(JsonWebKey jwk)
        {
            // RSA private key parameters
            if (!string.IsNullOrEmpty(jwk.D) ||
                !string.IsNullOrEmpty(jwk.P) ||
                !string.IsNullOrEmpty(jwk.Q) ||
                !string.IsNullOrEmpty(jwk.DP) ||
                !string.IsNullOrEmpty(jwk.DQ) ||
                !string.IsNullOrEmpty(jwk.QI))
            {
                return true;
            }

            return false;
        }
    }
}
