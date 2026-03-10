using Fhi.Authentication.JwtDPoP.Validation.Models;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Fhi.Authentication.JwtDPoP.Validation.DPoPProofValidators
{
    internal class IatLifetimeValidator : IDPoPProofValidator
    {
        private readonly TimeProvider _timeProvider;
        private readonly ILogger<IatLifetimeValidator> _logger;

        public IatLifetimeValidator(TimeProvider timeProvider, ILogger<IatLifetimeValidator> logger)
        {
            _timeProvider = timeProvider;
            _logger = logger;
        }

        public Task<DPoPValidationResult> ExecuteAsync(DPoPValidationContext context, JsonWebToken proofToken, CancellationToken cancellationToken = default)
        {
            var iatClaim = proofToken.Claims.FirstOrDefault(c => c.Type == DPoPConstants.IssuedAt);
            if (iatClaim == null)
                return Task.FromResult(new DPoPValidationResult(true, DPoPConstants.InvalidDPoPProof, DPoPErrorDescriptions.MissingRequiredClaimIat));

            if (!long.TryParse(iatClaim.Value, out var iat) || iat == 0)
                return Task.FromResult(new DPoPValidationResult(true, DPoPConstants.InvalidDPoPProof, DPoPErrorDescriptions.MissingRequiredClaimIat));

            var now = _timeProvider.GetUtcNow().ToUnixTimeSeconds();
            var skew = (long)context.ValidationParameters.AllowedClockSkew.TotalSeconds;

            if (iat > now + skew)
            {
                _logger.LogDebug("DPoP iat too far in the future. Diff: {diff}s", iat - now);
                return Task.FromResult(new DPoPValidationResult(true, DPoPConstants.InvalidDPoPProof, DPoPErrorDescriptions.IatTooFarInFuture));
            }

            var expiration = iat + (long)context.ValidationParameters.ProofTokenLifetimeValidationDuration.TotalSeconds;
            if (expiration < now - skew)
            {
                _logger.LogDebug("DPoP iat expired. Diff: {diff}s", now - expiration);
                return Task.FromResult(new DPoPValidationResult(true, DPoPConstants.InvalidDPoPProof, DPoPErrorDescriptions.IatTooOld));
            }

            return Task.FromResult(new DPoPValidationResult(false));
        }
    }
}
