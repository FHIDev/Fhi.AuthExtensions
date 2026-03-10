using Fhi.Authentication.JwtDPoP.Validation.Models;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Fhi.Authentication.JwtDPoP.Validation.DPoPProofValidators
{
    internal class JwtSignatureValidator : IDPoPProofValidator
    {
        private static readonly JsonWebTokenHandler _handler = new();

        public async Task<DPoPValidationResult> ExecuteAsync(DPoPValidationContext context, JsonWebToken proofToken, CancellationToken cancellationToken = default)
        {
            var jwk = proofToken.GetJwk();
            if (jwk != null)
            {
                var tokenResult = await _handler.ValidateTokenAsync(proofToken, new TokenValidationParameters
                {
                    RequireSignedTokens = true,
                    ValidateAudience = false,
                    ValidateIssuer = false,
                    ValidateLifetime = false,
                    ValidTypes = [DPoPConstants.DPoPProofTokenType],
                    IssuerSigningKey = jwk
                });
                if (!tokenResult.IsValid)
                {
                    if (tokenResult.Exception is SecurityTokenSignatureKeyNotFoundException ||
                        tokenResult.Exception is SecurityTokenInvalidSignatureException)
                    {
                        return new DPoPValidationResult(true, DPoPConstants.InvalidDPoPProof, DPoPErrorDescriptions.InvalidSignature);
                    }
                    else if (tokenResult.Exception is SecurityTokenInvalidTypeException)
                    {
                        return new DPoPValidationResult(true, DPoPConstants.InvalidDPoPProof, DPoPErrorDescriptions.InvalidTyp);
                    }
                    else
                    {
                        return new DPoPValidationResult(true, DPoPConstants.InvalidDPoPProof, "Unknown error");
                    }
                }

                return new DPoPValidationResult(false);
            }

            return new DPoPValidationResult(true, DPoPConstants.InvalidDPoPProof, DPoPErrorDescriptions.MalformedJwt);
        }
    }
}
