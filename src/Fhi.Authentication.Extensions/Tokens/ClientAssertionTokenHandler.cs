using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

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
        /// <param name="issuer">This value is the audience, but should be set as the OIDC issues</param>
        /// <param name="clientId">client identifier</param>
        /// <param name="jwk">json web key string</param>
        /// <param name="expiration">Optional expiration time. If null, defaults to 10 seconds from now.</param>
        /// <param name="kid">key identifier</param>
        /// <returns></returns>
        public static string CreateJwtToken(string issuer, string clientId, string jwk, DateTime? expiration = null, string? kid = null)
        {
            var securityKey = new JsonWebKey(jwk);
            var token = CreateJwtToken(issuer, clientId, securityKey, expiration, kid);

            return token;
        }

        private static string CreateJwtToken(string issuer, string clientId, JsonWebKey securityKey, DateTime? expiration = null, string? kid = null)
        {
            var claims = new List<Claim>
            {

                new(JwtRegisteredClaimNames.Sub, clientId),
                new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            };
            var payload = new JwtPayload(clientId, issuer, claims, DateTime.UtcNow, expiration ?? DateTime.UtcNow.AddSeconds(10));

            if (string.IsNullOrEmpty(securityKey.Alg))
                securityKey.Alg = SecurityAlgorithms.RsaSha256;
            securityKey.KeyId = kid ?? securityKey.Kid;
            var signingCredentials = new SigningCredentials(securityKey, securityKey.Alg);
            var header = new JwtHeader(signingCredentials, null, "client-authentication+jwt");

            var jwtSecurityToken = new JwtSecurityToken(header, payload);
            var token = new JwtSecurityTokenHandler().WriteToken(jwtSecurityToken);
            return token;
        }
    }
}
