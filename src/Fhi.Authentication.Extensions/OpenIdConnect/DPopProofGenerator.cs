using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text;
using Duende.IdentityModel;
using Microsoft.IdentityModel.Tokens;

namespace Fhi.Authentication.OpenIdConnect
{
    // TODO: Need to go through this. Copied from HelseId samples and Duende. Should we handle multiple algorithms?
    // need to figure out what algs, size etc. to support

    internal static class DPoPProofGenerator
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="url"></param>
        /// <param name="httpMethod"></param>
        /// <param name="key">public and private key</param>
        /// <param name="keyAlgorithm"></param>
        /// <param name="dPoPNonce"></param>
        /// <param name="accessToken"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static string CreateDPoPProof(string url, string httpMethod, string key, string keyAlgorithm, string? dPoPNonce = null, string? accessToken = null)
        {
            var securityKey = new JsonWebKey(key);
            var signingCredentials = new SigningCredentials(securityKey, keyAlgorithm);

            var jwk = securityKey.Kty switch
            {
                JsonWebAlgorithmsKeyTypes.EllipticCurve => new Dictionary<string, string>
                {
                    [JsonWebKeyParameterNames.Kty] = securityKey.Kty,
                    [JsonWebKeyParameterNames.X] = securityKey.X,
                    [JsonWebKeyParameterNames.Y] = securityKey.Y,
                    [JsonWebKeyParameterNames.Crv] = securityKey.Crv,
                },
                JsonWebAlgorithmsKeyTypes.RSA => new Dictionary<string, string>
                {
                    [JsonWebKeyParameterNames.Kty] = securityKey.Kty,
                    [JsonWebKeyParameterNames.N] = securityKey.N,
                    [JsonWebKeyParameterNames.E] = securityKey.E,
                },
                _ => throw new InvalidOperationException("Invalid key type for DPoP proof.")
            };

            var jwtHeader = new JwtHeader(signingCredentials)
            {
                [JwtClaimTypes.TokenType] = "dpop+jwt",
                [JwtClaimTypes.JsonWebKey] = jwk,
            };

            var payload = new JwtPayload
            {
                [JwtClaimTypes.JwtId] = Guid.NewGuid().ToString(),
                [JwtClaimTypes.DPoPHttpMethod] = httpMethod,
                [JwtClaimTypes.DPoPHttpUrl] = url,
                [JwtClaimTypes.IssuedAt] = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            };

            // Used when accessing the authentication server (HelseID):
            if (!string.IsNullOrEmpty(dPoPNonce))
            {
                // nonce: A recent nonce provided via the DPoP-Nonce HTTP header.
                payload[JwtClaimTypes.Nonce] = dPoPNonce;
            }

            // Used when accessing an API that requires a DPoP token:
            if (!string.IsNullOrEmpty(accessToken))
            {
                // ath: hash of the access token. The value MUST be the result of a base64url encoding
                // the SHA-256 [SHS] hash of the ASCII encoding of the associated access token's value.
                using var sha256 = SHA256.Create();
                var hash = sha256.ComputeHash(Encoding.ASCII.GetBytes(accessToken));
                var ath = Base64Url.Encode(hash);

                payload[JwtClaimTypes.DPoPAccessTokenHash] = ath;
            }

            var jwtSecurityToken = new JwtSecurityToken(jwtHeader, payload);
            return new JwtSecurityTokenHandler().WriteToken(jwtSecurityToken);
        }
    }
}