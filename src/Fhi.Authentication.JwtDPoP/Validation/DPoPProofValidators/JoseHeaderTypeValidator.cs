using Fhi.Authentication.JwtDPoP.Validation.Models;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Fhi.Authentication.JwtDPoP.Validation.DPoPProofValidators
{
    internal class JoseHeaderTypeValidator : IDPoPProofValidator
    {
        public Task<DPoPValidationResult> ExecuteAsync(DPoPValidationContext context, JsonWebToken? proofToken, CancellationToken cancellationToken = default)
        {
            if (!string.Equals(proofToken!.Typ, DPoPConstants.DPoPProofTokenType, StringComparison.OrdinalIgnoreCase))
                return Task.FromResult(new DPoPValidationResult(true, DPoPConstants.InvalidDPoPProof, DPoPErrorDescriptions.InvalidTyp));

            return Task.FromResult(new DPoPValidationResult(false));
        }
    }
}
