using Duende.IdentityModel;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

public static class TokenHandlers
{
    /// <summary>
    /// Client assertion token
    /// </summary>
    /// <param name="issuer"></param>
    /// <param name="clientId"></param>
    /// <param name="jwk"></param>
    /// <returns></returns>
    public static string CreateJwtToken(string issuer, string clientId, string jwk)
    {
        var securityKey = new JsonWebKey(jwk);
        //Create payload
        var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, clientId),
                new(JwtRegisteredClaimNames.Iat, DateTimeOffset.Now.ToUnixTimeSeconds().ToString()),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            };
        var payload = new JwtPayload(clientId, issuer, claims, DateTime.UtcNow, DateTime.UtcNow.AddSeconds(60));

        //Create header
        if (string.IsNullOrEmpty(securityKey.Alg))
            securityKey.Alg = SecurityAlgorithms.RsaSha256;
        var signingCredentials = new SigningCredentials(securityKey, securityKey.Alg);
        var header = new JwtHeader(signingCredentials, null, "client-authentication+jwt");

        //create signed JWT with header and payload 
        var jwtSecurityToken = new JwtSecurityToken(header, payload);
        var token = new JwtSecurityTokenHandler().WriteToken(jwtSecurityToken);
        return token;
    }

    public static string CreateDPoPProof(string url, string httpMethod, string key, string? dPoPNonce = null, string? accessToken = null)
    {
        var securityKey = new JsonWebKey(key);
        var signingCredentials = new SigningCredentials(securityKey, securityKey.Alg);

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

    public static string CreateDPoPKey()
    {
        var key = new RsaSecurityKey(RSA.Create(2048));
        var jwk = JsonWebKeyConverter.ConvertFromRSASecurityKey(key);
        jwk.Alg = "PS256";
        var jwkJson = JsonSerializer.Serialize(jwk);
        return jwkJson;
    }

}