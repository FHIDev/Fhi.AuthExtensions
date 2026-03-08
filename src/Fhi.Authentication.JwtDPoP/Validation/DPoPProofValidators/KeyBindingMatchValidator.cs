using Fhi.Authentication.JwtDPoP.Validation;
using Fhi.Authentication.JwtDPoP.Validation.Models;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Text.Json;

namespace Fhi.Authentication.JwtDPoP.Validators.DPoPProof
{
    internal class KeyBindingMatchValidator : IDPoPProofValidators
    {
        private readonly ILogger<KeyBindingMatchValidator> _logger;

        public KeyBindingMatchValidator(ILogger<KeyBindingMatchValidator> logger)
        {
            _logger = logger;
        }

        public Task<DpopValidationResult> ExecuteAsync(DPoPValidationContext context, JsonWebToken? proofToken, CancellationToken cancellationToken = default)
        {
            var cnf = context.AccessTokenClaims.FirstOrDefault(c => c.Type == DPoPConstants.Confirmation);

            if (cnf == null || string.IsNullOrEmpty(cnf.Value))
            {
                _logger.LogDebug("Missing cnf claim in access token.");
                return Task.FromResult(new DpopValidationResult(true, DPoPConstants.InvalidDPoPProof, DPoPErrorDescriptions.KeyBindingMismatch));
            }

            try
            {
                var cnfJson = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(cnf.Value);
                if (cnfJson == null || !cnfJson.TryGetValue(DPoPConstants.ConfirmationMethodJwkThumbprint, out var jktJson))
                {
                    _logger.LogDebug("Missing jkt in cnf claim.");
                    return Task.FromResult(new DpopValidationResult(true, DPoPConstants.InvalidDPoPProof, DPoPErrorDescriptions.KeyBindingMismatch));
                }

                var accessTokenJkt = jktJson.ToString();
                var proofJkt = proofToken?.GetJwk()?.ComputeJwkThumbprint();

                if (accessTokenJkt != Base64UrlEncoder.Encode(proofJkt))
                {
                    _logger.LogDebug("cnf jkt does not match proof key thumbprint.");
                    return Task.FromResult(new DpopValidationResult(true, DPoPConstants.InvalidDPoPProof, DPoPErrorDescriptions.KeyBindingMismatch));
                }

                return Task.FromResult(new DpopValidationResult(false));
            }
            catch (JsonException ex)
            {
                _logger.LogDebug("Failed to parse cnf claim: {Error}", ex.Message);
                return Task.FromResult(new DpopValidationResult(true, DPoPConstants.InvalidDPoPProof, DPoPErrorDescriptions.KeyBindingMismatch));
            }
        }
    }
}
