using Fhi.Authentication.JwtDPoP.Validation.Models;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Fhi.Authentication.JwtDPoP.Validation.DPoPProofValidators
{
    internal class HttpMethodMatchValidator : IDPoPProofValidator
    {
        public Task<DPoPValidationResult> ExecuteAsync(DPoPValidationContext context, JsonWebToken proofToken, CancellationToken cancellationToken = default)
        {
            var htm = proofToken.Claims.FirstOrDefault(c => c.Type == DPoPConstants.DPoPHttpMethod)?.Value;

            if (string.IsNullOrEmpty(htm))
                return Task.FromResult(new DPoPValidationResult(true, DPoPConstants.InvalidDPoPProof, DPoPErrorDescriptions.MissingRequiredClaimHtm));

            if (!context.ExpectedMethod.Equals(htm, StringComparison.OrdinalIgnoreCase))
                return Task.FromResult(new DPoPValidationResult(true, DPoPConstants.InvalidDPoPProof, DPoPErrorDescriptions.HtmMismatch));

            return Task.FromResult(new DPoPValidationResult(false));
        }
    }
}
