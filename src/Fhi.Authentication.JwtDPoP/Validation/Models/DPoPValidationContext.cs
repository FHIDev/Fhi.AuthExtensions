using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Fhi.Authentication.JwtDPoP.Validation.Models
{
    internal record DPoPProofRequestValidationContext(HttpRequest Request, DPoPProofTokenValidationParameters ValidationParameters);
    internal record DPoPValidationContext
    {
        public required string ExpectedUrl { get; init; }
        public required string ExpectedMethod { get; init; }
        public required string ProofToken { get; init; }
        public required string AccessToken { get; init; }
        public IEnumerable<Claim> AccessTokenClaims { get; init; } = Array.Empty<Claim>();

        public required DPoPProofTokenValidationParameters ValidationParameters { get; init; }

        public int DPoPHeaderCount { get; init; }
    }
}
