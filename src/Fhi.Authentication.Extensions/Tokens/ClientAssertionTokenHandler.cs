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
        /// <returns></returns>
        public static string CreateJwtToken(string issuer, string clientId, string jwk)
        {
            var securityKey = new JsonWebKey(jwk);
            var token = CreateJwtToken(issuer, clientId, securityKey);

            return token;
        }

        //public static string CreateJwtToken(string issuer, string clientId, string certificatePath, string certificatePassword)
        //{
        //    var certificate = new X509Certificate2(certificatePath, certificatePassword);
        //    var securityKey = new X509SecurityKey(certificate);
        //    string token = CreateJwtToken(issuer, clientId, securityKey);
        //    return token;
        //}

        private static string CreateJwtToken(string issuer, string clientId, JsonWebKey securityKey)
        {
            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, clientId),
                new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            };
            var payload = new JwtPayload(clientId, issuer, claims, DateTime.UtcNow, DateTime.UtcNow.AddSeconds(60));

            if (string.IsNullOrEmpty(securityKey.Alg))
                securityKey.Alg = SecurityAlgorithms.RsaSha256;
            var signingCredentials = new SigningCredentials(securityKey, securityKey.Alg);
            var header = new JwtHeader(signingCredentials, null, "client-authentication+jwt");

            var jwtSecurityToken = new JwtSecurityToken(header, payload);
            var token = new JwtSecurityTokenHandler().WriteToken(jwtSecurityToken);
            return token;
        }
    }
}
