using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

namespace Fhi.Authentication.Tokens
{
    //TODO: Complete DPoP proof creation
    internal interface IDPoPProofHandler
    {
        public string CreateDPoPProof(string url, string httpMethod, string key);
        //public string CreateDPoPProofWithNonce(string url, string httpMethod, string jwk, string dPoPNonce);
        //public string CreateDPoPProofWithAccessToken(string url, string httpMethod, string jwk, string accessToken);
    }

    internal class DefaultDPoPProofHandler : IDPoPProofHandler
    {
        public string CreateDPoPProof(string url, string httpMethod, string jwk)
        {
            var securityKey = new JsonWebKey(jwk);
            var signingCredentials = new SigningCredentials(securityKey, securityKey.Alg);

            //TODO: Go through supported algs with HelseId and MS
            var jwkHeaderKey = securityKey.Kty switch
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
                //[JwtClaimTypes.TokenType] = "dpop+jwt",
                //[JwtClaimTypes.JsonWebKey] = jwk,
            };

            var payload = new JwtPayload
            {
                //[JwtClaimTypes.JwtId] = Guid.NewGuid().ToString(),
                //[JwtClaimTypes.DPoPHttpMethod] = httpMethod,
                //[JwtClaimTypes.DPoPHttpUrl] = url,
                //[JwtClaimTypes.IssuedAt] = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            };
            var jwtSecurityToken = new JwtSecurityToken(jwtHeader, payload);
            return new JwtSecurityTokenHandler().WriteToken(jwtSecurityToken);
        }

        public string CreateDPoPProofWithAccessToken(string url, string httpMethod, string jwk, string accessToken)
        {
            throw new NotImplementedException();
        }

        public string CreateDPoPProofWithNonce(string url, string httpMethod, string jwk, string dPoPNonce)
        {
            throw new NotImplementedException();
        }
    }
}
