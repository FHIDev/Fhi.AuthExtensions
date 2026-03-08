using Fhi.Authentication.JwtDPoP.Validation.DPoPProofValidators;
using Fhi.Authentication.JwtDPoP.Validation.Models;
using Fhi.Authentication.JwtDPoP.Validators.DPoPProof;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Fhi.Authentication.JwtDPoP.Validation
{
    internal class DPoPProofCompositeValidator
    {
        private readonly List<IDPoPProofValidators> _validators = new();

        public DPoPProofCompositeValidator(
            JwtSignatureValidator signatureValidator,
            JoseHeaderTypeValidator typ,
            JoseHeaderAlgorithmPolicyValidator alg,
            JoseHeaderJwkValidator keyPresence,
            HttpMethodMatchValidator htm,
            HttpUriMatchValidator htu,
            IatLifetimeValidator iat,
            AthMatchValidator ath,
            KeyBindingMatchValidator cnf,
            JtiLengthGuardValidator jtiLength,
            JtiReplayValidator jtiReplay)
        {
            // AddValidator(typ);
            AddValidator(signatureValidator);
            AddValidator(alg);
            AddValidator(keyPresence);
            AddValidator(htm);
            AddValidator(htu);
            AddValidator(iat);
            AddValidator(ath);
            AddValidator(cnf);
            AddValidator(jtiLength);
            AddValidator(jtiReplay);
        }

        private void AddValidator(IDPoPProofValidators step)
        {
            _validators.Add(step);
        }

        public async Task<DpopValidationResult> ExecuteValidatorsAsync(
            DPoPValidationContext context,
            JsonWebToken proofToken,
            CancellationToken cancellationToken = default)
        {
            foreach (var validator in _validators)
            {
                var result = await validator.ExecuteAsync(context, proofToken, cancellationToken);
                if (result.IsError)
                    return result;
            }
            return new DpopValidationResult(false);
        }

    }
}
