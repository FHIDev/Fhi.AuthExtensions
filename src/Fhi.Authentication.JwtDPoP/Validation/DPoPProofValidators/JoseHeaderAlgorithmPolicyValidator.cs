using Fhi.Authentication.JwtDPoP.Validation.Models;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Fhi.Authentication.JwtDPoP.Validation.DPoPProofValidators
{
    internal class JoseHeaderAlgorithmPolicyValidator : IDPoPProofValidator
    {
        public Task<DPoPValidationResult> ExecuteAsync(DPoPValidationContext context, JsonWebToken proofToken, CancellationToken cancellationToken = default)
        {
            var alg = proofToken.Alg;
            if (!context.ValidationParameters.ValidAlgorithms.Contains(alg, StringComparer.OrdinalIgnoreCase))
            {
                return Task.FromResult(new DPoPValidationResult(true, DPoPConstants.InvalidDPoPProof, DPoPErrorDescriptions.DisallowedAlg));
            }

            return Task.FromResult(new DPoPValidationResult(false));
        }
    }
}
