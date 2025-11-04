using System.Text;
using System.Text.Json;
using Microsoft.IdentityModel.Tokens;

namespace Fhi.Authentication.Tokens
{
    /// <summary>
    /// 
    /// </summary>
    public static class JsonWebKeyExtensions
    {
        private static readonly HashSet<string> ExcludedPublicProps =
            new(StringComparer.OrdinalIgnoreCase) { "oth", "x5c", "key_ops" };

        /// <summary>
        /// Serializes the JsonWebKey and filters out parameters to create a "public" key.
        /// </summary>
        /// <param name="jwk"></param>
        /// <returns></returns>
        public static string ToFilteredJson(this JsonWebKey jwk)
        {
            var rawJson = JsonSerializer.Serialize(jwk);
            using var doc = JsonDocument.Parse(rawJson);
            using var stream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(stream))
            {
                writer.WriteStartObject();
                foreach (var prop in doc.RootElement.EnumerateObject())
                {
                    if (!ExcludedPublicProps.Contains(prop.Name))
                        prop.WriteTo(writer);
                }
                writer.WriteEndObject();
            }
            return Encoding.UTF8.GetString(stream.ToArray());
        }
    }
}
