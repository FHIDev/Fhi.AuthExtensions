using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Fhi.Authentication.Tokens
{
    /// <summary>
    /// Generate Json Web Tokens (JWT) for client assertion.
    /// </summary>
    public static class ClientAssertionTokenHandler
    {
        /// <summary>
        /// Create a JWT token for client assertion.
        /// </summary>
        /// <param name="issuer">This value is the audience, but should be set as the OIDC issuer</param>
        /// <param name="clientId">client identifier</param>
        /// <param name="jwk">json web key string</param>
        /// <param name="expiration">Optional expiration time. If null, defaults to 10 seconds from <paramref name="utcNow"/>.</param>
        /// <param name="kid">key identifier</param>
        /// <param name="utcNow">Optional current UTC time used for iat, nbf, and default exp claims. If null, defaults to <see cref="DateTimeOffset.UtcNow"/>.</param>
        /// <returns></returns>
        public static string CreateJwtToken(string issuer, string clientId, string jwk, DateTime? expiration = null, string? kid = null, DateTimeOffset? utcNow = null)
        {
            var securityKey = new JsonWebKey(jwk);
            var token = CreateJwtToken(issuer, clientId, securityKey, utcNow ?? DateTimeOffset.UtcNow, expiration, kid);
            return token;
        }

        private static string CreateJwtToken(string issuer, string clientId, JsonWebKey securityKey, DateTimeOffset utcNow, DateTime? expiration = null, string? kid = null)
        {
            // JsonWebTokenHandler does not validate that Expires > NotBefore (unlike the old JwtSecurityTokenHandler which threw IDX12401).
            // We validate explicitly since ExpirationSeconds is user-configurable.
            var expires = expiration ?? utcNow.AddSeconds(10).UtcDateTime;
            if (expires <= utcNow.UtcDateTime)
                throw new ArgumentException($"Expiration ({expires:O}) must be after the current time ({utcNow.UtcDateTime:O}).", nameof(expiration));

            if (string.IsNullOrEmpty(securityKey.Alg))
                securityKey.Alg = SecurityAlgorithms.RsaSha256;
            securityKey.KeyId = kid ?? securityKey.Kid;

            var descriptor = new SecurityTokenDescriptor
            {
                Issuer = clientId,
                Audience = issuer,
                NotBefore = utcNow.UtcDateTime,
                Expires = expires,
                SigningCredentials = new SigningCredentials(securityKey, securityKey.Alg),
                Claims = new Dictionary<string, object>
                {
                    { JwtRegisteredClaimNames.Sub, clientId },
                    { JwtRegisteredClaimNames.Iat, utcNow.ToUnixTimeSeconds() },
                    { JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N") },
                },
                AdditionalHeaderClaims = new Dictionary<string, object>
                {
                    { JwtHeaderParameterNames.Typ, "client-authentication+jwt" },
                },
            };

            // Prevent the handler from auto-generating iat, nbf, exp — we set them explicitly above
            var handler = new JsonWebTokenHandler { SetDefaultTimesOnTokenCreation = false };
            return handler.CreateToken(descriptor);
        }
    }
}
