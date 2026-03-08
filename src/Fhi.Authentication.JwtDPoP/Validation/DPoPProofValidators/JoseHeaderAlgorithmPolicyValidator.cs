using Fhi.Authentication.JwtDPoP.Validation;
using Fhi.Authentication.JwtDPoP.Validation.Models;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Fhi.Authentication.JwtDPoP.Validators.DPoPProof
{
    internal class JoseHeaderAlgorithmPolicyValidator : IDPoPProofValidators
    {
        public Task<DpopValidationResult> ExecuteAsync(DPoPValidationContext context, JsonWebToken? proofToken, CancellationToken cancellationToken = default)
        {
            var alg = proofToken?.Alg;
            if (!context.ValidationParameters.ValidAlgorithms.Contains(alg, StringComparer.OrdinalIgnoreCase))
            {
                return Task.FromResult(new DpopValidationResult(true, DPoPConstants.InvalidDPoPProof, DPoPErrorDescriptions.DisallowedAlg));
            }

            return Task.FromResult(new DpopValidationResult(false));
        }
    }
}
