using Fhi.Authentication.JwtDPoP.Validation.Models;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Fhi.Authentication.JwtDPoP.Validation.DPoPProofValidators
{
    internal class JtiLengthGuardValidator : IDPoPProofValidator
    {
        public Task<DPoPValidationResult> ExecuteAsync(DPoPValidationContext context, JsonWebToken proofToken, CancellationToken cancellationToken = default)
        {
            var jti = proofToken.Claims.FirstOrDefault(c => c.Type == DPoPConstants.JwtId)?.Value;

            if (string.IsNullOrEmpty(jti))
                return Task.FromResult(new DPoPValidationResult(true, DPoPConstants.InvalidDPoPProof, DPoPErrorDescriptions.MissingRequiredClaimJti));

            if (jti.Length > context.ValidationParameters.MaxJtiLength)
                return Task.FromResult(new DPoPValidationResult(true, DPoPConstants.InvalidDPoPProof, DPoPErrorDescriptions.JtiTooLong));

            return Task.FromResult(new DPoPValidationResult(false));
        }
    }
}
