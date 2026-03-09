using Fhi.Authentication.JwtDPoP.Validation;
using Fhi.Authentication.JwtDPoP.Validation.Models;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using System.Text;

namespace Fhi.Authentication.JwtDPoP.Validation.DPoPProofValidators
{
    internal class JtiReplayValidator : IDPoPProofValidators
    {
        private readonly IReplayCache _replayCache;
        private readonly TimeProvider _timeProvider;
        private readonly ILogger<JtiReplayValidator> _logger;

        public JtiReplayValidator(IReplayCache replayCache, TimeProvider timeProvider, ILogger<JtiReplayValidator> logger)
        {
            _replayCache = replayCache;
            _timeProvider = timeProvider;
            _logger = logger;
        }

        public async Task<DpopValidationResult> ExecuteAsync(DPoPValidationContext context, JsonWebToken? proofToken, CancellationToken cancellationToken = default)
        {
            var jti = proofToken!.Claims.FirstOrDefault(c => c.Type == DPoPConstants.JwtId)?.Value;
            if (jti != null)
            {
                var jtiBytes = Encoding.UTF8.GetBytes(jti);
                var tokenIdHash = Base64UrlEncoder.Encode(SHA256.HashData(jtiBytes));

                if (await _replayCache.Exists(tokenIdHash, cancellationToken))
                {
                    _logger.LogDebug("DPoP jti replay detected.");
                    return new DpopValidationResult(true, DPoPConstants.InvalidDPoPProof, DPoPErrorDescriptions.JtiReplay);
                }

                var options = context.ValidationParameters;
                var skew = TimeSpan.Zero;
                skew = options.AllowedClockSkew;
                skew *= 2;

                var cacheDuration = options.ProofTokenLifetimeValidationDuration + skew;
                var expiration = _timeProvider.GetUtcNow().Add(cacheDuration);
                await _replayCache.Add(tokenIdHash, expiration, cancellationToken);

                return new DpopValidationResult(false);
            }

            return new DpopValidationResult(true, DPoPConstants.InvalidDPoPProof, DPoPErrorDescriptions.MissingRequiredClaim);
        }
    }
}
