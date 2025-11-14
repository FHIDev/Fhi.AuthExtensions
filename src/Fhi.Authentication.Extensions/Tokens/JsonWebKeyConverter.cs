using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.IdentityModel.Tokens
{
    /// <summary>
    /// Custom converter that serializes only the public JWK fields.
    /// </summary>
    public class PublicJsonWebKeyConverter : JsonConverter<JsonWebKey>
    {
        /// <inheritdoc/>
        public override JsonWebKey? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // Returns the normal deserializing process
            return JsonSerializer.Deserialize<JsonWebKey>(ref reader, options);
        }

        /// <summary>
        /// Only writes the public JWK value fields and skips any other fields present on a private JsonWebkey object.
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="value"></param>
        /// <param name="options"></param>
        public override void Write(Utf8JsonWriter writer, JsonWebKey value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            if (value.Kty is not null) writer.WriteString("kty", value.Kty);
            if (value.N is not null) writer.WriteString("n", value.N);
            if (value.E is not null) writer.WriteString("e", value.E);
            if (value.Alg is not null) writer.WriteString("alg", value.Alg);
            if (value.Use is not null) writer.WriteString("use", value.Use);
            if (value.Kid is not null) writer.WriteString("kid", value.Kid);
            if (value.X5u is not null) writer.WriteString("x5u", value.X5u);

            writer.WriteEndObject();
        }
    }
}
