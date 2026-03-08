using Fhi.Authentication.JwtDPoP.Validation;
using Fhi.Authentication.JwtDPoP.Validation.Models;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Fhi.Authentication.JwtDPoP.Validators.DPoPProof
{
    internal class HttpMethodMatchValidator : IDPoPProofValidators
    {
        public Task<DpopValidationResult> ExecuteAsync(DPoPValidationContext context, JsonWebToken? proofToken, CancellationToken cancellationToken = default)
        {
            var htm = proofToken!.Claims.FirstOrDefault(c => c.Type == DPoPConstants.DPoPHttpMethod)?.Value;

            if (string.IsNullOrEmpty(htm))
                return Task.FromResult(new DpopValidationResult(true, DPoPConstants.InvalidDPoPProof, DPoPErrorDescriptions.MissingRequiredClaim + " htm"));

            if (!context.ExpectedMethod.Equals(htm, StringComparison.OrdinalIgnoreCase))
                return Task.FromResult(new DpopValidationResult(true, DPoPConstants.InvalidDPoPProof, DPoPErrorDescriptions.HtmMismatch));

            return Task.FromResult(new DpopValidationResult(false));
        }
    }
}
