using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Text.Json;

namespace Fhi.Authentication.JwtDPoP
{
    internal static class JsonWebTokenExtensions
    {
        public static JsonWebKey? GetJwk(this JsonWebToken token)
        {
            if (token.TryGetHeaderValue<JsonElement>(DPoPConstants.JsonWebKey, out var jwkValues))
            {
                var jwkJson = JsonSerializer.Serialize(jwkValues);
                return new JsonWebKey(jwkJson);
            }

            return null;
        }
    }
}
