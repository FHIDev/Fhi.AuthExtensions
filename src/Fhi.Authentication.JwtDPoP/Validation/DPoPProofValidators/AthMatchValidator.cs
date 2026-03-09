using Fhi.Authentication.JwtDPoP.Validation;
using Fhi.Authentication.JwtDPoP.Validation.Models;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using System.Text;

namespace Fhi.Authentication.JwtDPoP.Validation.DPoPProofValidators
{
    internal class AthMatchValidator : IDPoPProofValidator
    {
        public Task<DPoPValidationResult> ExecuteAsync(DPoPValidationContext context, JsonWebToken? proofToken, CancellationToken cancellationToken = default)
        {
            var ath = proofToken!.Claims.FirstOrDefault(c => c.Type == DPoPConstants.DPoPAccessTokenHash)?.Value;
            if (string.IsNullOrEmpty(ath))
                return Task.FromResult(new DPoPValidationResult(true, DPoPConstants.InvalidDPoPProof, DPoPErrorDescriptions.MissingRequiredClaimAth));

            var expected = Base64UrlEncoder.Encode(SHA256.HashData(Encoding.UTF8.GetBytes(context.AccessToken)));
            if (expected != ath)
                return Task.FromResult(new DPoPValidationResult(true, DPoPConstants.InvalidDPoPProof, DPoPErrorDescriptions.AthMismatch));

            return Task.FromResult(new DPoPValidationResult(false));
        }
    }
}
