using Fhi.Authentication.JwtDPoP.Validation.Models;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Fhi.Authentication.JwtDPoP.Validation
{
    internal interface IDPoPProofHandler
    {
        Task<DpopValidationResult> ValidateRequest(DPoPProofRequestValidationContext context);
        Task<DpopValidationResult> ValidateDPoPProof(DPoPValidationContext context);
    }

    internal class DPoPHandler : IDPoPProofHandler
    {
        private static readonly JsonWebTokenHandler _handler = new();
        private readonly ILogger<DPoPHandler> _logger;
        private DPoPProofCompositeValidator _validator;

        public DPoPHandler(
            ILogger<DPoPHandler> logger,
            DPoPProofCompositeValidator validator)
        {
            _logger = logger;
            _validator = validator;
        }

        /// <summary>
        /// Validate DPoP proof jwt see https://www.rfc-editor.org/rfc/rfc9449.html#name-checking-dpop-proofs
        ///
        /// Header (JOSE) validation: JoseHeaderTypeValidator, AlgorithmPolicyValidator
        /// Http validation: HtmMatchValidator, HtuMatchValidator
        /// Proof lifetime duration: IatLifetimeValidator.
        /// Token‑binding: AthMatchValidator, KeyBindingMatchValidator
        /// Replay: JtiLengthGuardValidator, JtiReplayValidator
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task<DpopValidationResult> ValidateDPoPProof(DPoPValidationContext context)
        {
            JsonWebToken? token = null;
            try
            {
                token = _handler.ReadJsonWebToken(context.ProofToken);

                var result = await _validator.ExecuteValidatorsAsync(context, token);
                if (result.IsError)
                    _logger.LogDebug("DPoP validation failed: {Error}", result.ErrorDescription ?? result.Error);

                return result;
            }
            catch (Exception e)
            {
                if (e is SecurityTokenMalformedException)
                    return new DpopValidationResult(true, DPoPConstants.InvalidDPoPProof, DPoPErrorDescriptions.MalformedJwt);

                _logger.LogDebug("DPoP validation failed: {Error}", e.Message);
                return new DpopValidationResult(true, DPoPConstants.InvalidDPoPProof, "unknown");
            }
        }

        public Task<DpopValidationResult> ValidateRequest(DPoPProofRequestValidationContext context)
        {
            if (!context.Request.Headers.ContainsKey(DPoPConstants.DPoPHeaderName))
            {
                return Task.FromResult(new DpopValidationResult(true, DPoPConstants.InvalidRequest, "Missing DPOP header"));
            }

            context.Request.Headers.TryGetValue(DPoPConstants.DPoPHeaderName, out var token);

            if (token.Count > 1)
            {
                return Task.FromResult(new DpopValidationResult(true, DPoPConstants.InvalidRequest, "Multiple DPoP proof headers present"));
            }

            if (token.FirstOrDefault()?.Length >= context.ValidationParameters.ProofTokenMaxLength)
            {
                return Task.FromResult(new DpopValidationResult(true, DPoPConstants.InvalidRequest, "DPoP proof header exceeds maximum length"));
            }

            return Task.FromResult(new DpopValidationResult(false));
        }
    }
}
