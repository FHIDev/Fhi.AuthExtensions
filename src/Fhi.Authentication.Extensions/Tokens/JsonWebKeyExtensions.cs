using System.Text.Json;

namespace Microsoft.IdentityModel.Tokens
{
    internal static class JsonWebKeyExtensions
    {
        /// <summary>
        /// Serializes a private JsonWebkey object into a public JsonWebKey JSON string.
        /// </summary>
        /// <param name="privateJwk"></param>
        /// <returns>A JSON string with only public key values.</returns>
        internal static string SerializeToPublicJwk(this JsonWebKey privateJwk)
        {
            var publicJwkValues = new JsonWebKey
            {
                Alg = privateJwk.Alg,
                Kty = privateJwk.Kty,
                Kid = privateJwk.Kid,
                N = privateJwk.N,
                E = privateJwk.E,
                Use = privateJwk.Use
            };

            var publicOptions = new JsonSerializerOptions
            {
                // Converter ensures only public key values are added to the public key
                Converters = { new PublicJsonWebKeyConverter() },
                WriteIndented = false
            };

            string publicJwkJson = JsonSerializer.Serialize(publicJwkValues, publicOptions);

            return publicJwkJson;
        }
    }
}