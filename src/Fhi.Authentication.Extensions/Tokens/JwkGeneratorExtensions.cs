using System.Text;
using System.Text.Json;

namespace Microsoft.IdentityModel.Tokens
{
    /// <summary>
    /// Extensions for the Microsoft JsonWebKey object.
    /// </summary>
    internal static class JsonWebKeyExtensions
    {
        private static readonly HashSet<string> AllowedPublicProps =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "kty", "n", "e", "alg", "use", "kid", "x5u"
            };

        /// <summary>
        /// Certain private key fields are present as default when we use the JsonWebKey Microsoft object.
        /// This method ensures only the public key parameters are present when creating a public jwk.
        /// </summary>
        /// <param name="jwk"></param>
        /// <returns></returns>
        internal static string ToPublicJwk(this JsonWebKey jwk)
        {
            var rawJson = JsonSerializer.Serialize(jwk);
            using var doc = JsonDocument.Parse(rawJson);
            using var stream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(stream))
            {
                writer.WriteStartObject();
                foreach (var prop in doc.RootElement.EnumerateObject())
                {
                    if (AllowedPublicProps.Contains(prop.Name))
                    {
                        prop.WriteTo(writer);
                    }
                }
                writer.WriteEndObject();
            }
            return Encoding.UTF8.GetString(stream.ToArray());
        }
    }
}
