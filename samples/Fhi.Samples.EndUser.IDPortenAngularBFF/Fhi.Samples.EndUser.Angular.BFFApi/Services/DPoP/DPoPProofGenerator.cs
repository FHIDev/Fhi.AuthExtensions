using Fhi.Samples.EndUser.Angular.BFFApi.Services.Models;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

namespace Fhi.Samples.EndUser.Angular.BFFApi.Services.DPoP
{
    public class DPoPProofGenerator : IDPoPProofGenerator
    {
        public Task<string> CreateProofAsync(string method, string url, DpopKeyPair keyPair)
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            var payload = new JwtPayload
            {
                { "htu", url },
                { "htm", method },
                { "iat", now },
                { "jti", Guid.NewGuid().ToString() }
            };

            var header = new JwtHeader(
                new SigningCredentials(keyPair.PrivateKey, SecurityAlgorithms.RsaSha256)
            );

            header["jwk"] = keyPair.PublicJwk;

            var token = new JwtSecurityToken(header, payload);
            var handler = new JwtSecurityTokenHandler();

            return Task.FromResult(handler.WriteToken(token));
        }
    }
}