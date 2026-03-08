using Fhi.Authentication.JwtDPoP.Validation;
using Fhi.Authentication.JwtDPoP.Validation.Models;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using System.Text;

namespace Fhi.Authentication.JwtDPoP.Validators.DPoPProof
{
    internal class AthMatchValidator : IDPoPProofValidators
    {
        public Task<DpopValidationResult> ExecuteAsync(DPoPValidationContext context, JsonWebToken? proofToken, CancellationToken cancellationToken = default)
        {
            var ath = proofToken!.Claims.FirstOrDefault(c => c.Type == DPoPConstants.DPoPAccessTokenHash)?.Value;
            if (string.IsNullOrEmpty(ath))
                return Task.FromResult(new DpopValidationResult(true, DPoPConstants.InvalidDPoPProof, DPoPErrorDescriptions.MissingRequiredClaim + " ath"));

            var expected = Base64UrlEncoder.Encode(SHA256.HashData(Encoding.UTF8.GetBytes(context.AccessToken)));
            if (expected != ath)
                return Task.FromResult(new DpopValidationResult(true, DPoPConstants.InvalidDPoPProof, DPoPErrorDescriptions.AthMismatch));

            return Task.FromResult(new DpopValidationResult(false));
        }
    }
}
