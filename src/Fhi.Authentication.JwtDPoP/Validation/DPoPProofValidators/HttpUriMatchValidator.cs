using Fhi.Authentication.JwtDPoP.Validation;
using Fhi.Authentication.JwtDPoP.Validation.Models;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Fhi.Authentication.JwtDPoP.Validation.DPoPProofValidators
{
    internal class HttpUriMatchValidator : IDPoPProofValidators
    {
        public Task<DpopValidationResult> ExecuteAsync(DPoPValidationContext context, JsonWebToken? proofToken, CancellationToken cancellationToken = default)
        {
            var htu = proofToken!.Claims.FirstOrDefault(c => c.Type == DPoPConstants.DPoPHttpUrl)?.Value;

            if (string.IsNullOrEmpty(htu))
                return Task.FromResult(new DpopValidationResult(true, DPoPConstants.InvalidDPoPProof, DPoPErrorDescriptions.MissingRequiredClaimHtu));

            if (!HtuIsValid(context.ExpectedUrl, htu))
                return Task.FromResult(new DpopValidationResult(true, DPoPConstants.InvalidDPoPProof, DPoPErrorDescriptions.HtuMismatch));

            return Task.FromResult(new DpopValidationResult(false));
        }

        private static bool HtuIsValid(string expectedUrl, string htu)
        {
            if (string.IsNullOrEmpty(expectedUrl))
                return false;

            try
            {
                return Uri.Compare(
                    new Uri(expectedUrl),
                    new Uri(htu),
                    UriComponents.Scheme | UriComponents.HostAndPort | UriComponents.Path,
                    UriFormat.SafeUnescaped,
                    StringComparison.OrdinalIgnoreCase) == 0;
            }
            catch (UriFormatException)
            {
                return false;
            }
        }
    }
}
