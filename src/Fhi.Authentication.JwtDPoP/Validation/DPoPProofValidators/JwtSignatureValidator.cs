using Fhi.Authentication.JwtDPoP.Validation.Models;
using Fhi.Authentication.JwtDPoP.Validators.DPoPProof;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Fhi.Authentication.JwtDPoP.Validation.DPoPProofValidators
{
    internal class JwtSignatureValidator : IDPoPProofValidators
    {
        public async Task<DpopValidationResult> ExecuteAsync(DPoPValidationContext context, JsonWebToken? proofToken, CancellationToken cancellationToken = default)
        {
            var jwk = proofToken?.GetJwk();
            if (jwk != null)
            {
                var handler = new JsonWebTokenHandler();
                var tokenResult = await handler.ValidateTokenAsync(proofToken, new TokenValidationParameters
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
                        return new DpopValidationResult(true, DPoPConstants.InvalidDPoPProof, DPoPErrorDescriptions.InvalidSignature);
                    }
                    else if (tokenResult.Exception is SecurityTokenInvalidTypeException)
                    {
                        return new DpopValidationResult(true, DPoPConstants.InvalidDPoPProof, DPoPErrorDescriptions.InvalidTyp);
                    }
                    else
                    {
                        return new DpopValidationResult(true, DPoPConstants.InvalidDPoPProof, "Unknown error");
                    }
                }

                return new DpopValidationResult(false);
            }

            return new DpopValidationResult(true, DPoPConstants.InvalidDPoPProof, DPoPErrorDescriptions.MalformedJwt);
        }
    }
}
