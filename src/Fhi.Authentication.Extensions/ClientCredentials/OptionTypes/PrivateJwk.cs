using System.Text;
using System.Text.Json;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Represents a JSON Web Key (JWK) that contains private key information.  
    /// </summary>
    /// <remarks>This struct provides implicit conversion to and from <see cref="string"/>, allowing easy
    /// manipulation of the JWK as a string.</remarks>
    public readonly struct PrivateJwk
    {
        private readonly string _json;

        private PrivateJwk(string json)
        {
            _json = json;
        }

        /// <summary>
        /// Parses a JSON string to create a <see cref="PrivateJwk"/> instance.
        /// </summary>
        /// <param name="json">The JSON string representing the JWK. Must not be null or empty.</param>
        /// <returns>A <see cref="PrivateJwk"/> object initialized with the provided JSON data.</returns>
        /// <exception cref="ArgumentException">Thrown if <paramref name="json"/> is null, empty, or not in a valid JWK JSON format.</exception>
        public static PrivateJwk ParseFromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new ArgumentException("JWK JSON cannot be null or empty.", nameof(json));

            try
            {
                JsonDocument.Parse(json);
            }
            catch (JsonException ex)
            {
                throw new ArgumentException("Invalid JWK JSON format.", ex);
            }
            return new PrivateJwk(json.Trim());
        }

        /// <summary>
        /// Parses a PEM-encoded RSA private key and converts it to a JWK (JSON Web Key) format.
        /// </summary>
        /// <param name="pem">The PEM-encoded RSA private key string. Must not be null or empty.</param>
        /// <returns>A <see cref="PrivateJwk"/> object initialized with the JWK representation of the key.</returns>
        /// <exception cref="ArgumentException">Thrown if <paramref name="pem"/> is null, empty, or not a valid PEM format.</exception>
        public static PrivateJwk ParseFromPem(string pem)
        {
            if (string.IsNullOrWhiteSpace(pem))
                throw new ArgumentException("PEM cannot be null or empty.", nameof(pem));

            try
            {
                using var rsa = System.Security.Cryptography.RSA.Create();
                rsa.ImportFromPem(pem);
                
                var rsaParameters = rsa.ExportParameters(true);
                var jwk = new RsaSecurityKey(rsaParameters);
                
                var jsonWebKey = JsonWebKeyConverter.ConvertFromRSASecurityKey(jwk);
                var jwkJson = JsonSerializer.Serialize(jsonWebKey);
                
                return new PrivateJwk(jwkJson.Trim());
            }
            catch (Exception ex) when (ex is not ArgumentException)
            {
                throw new ArgumentException("Invalid PEM format or unable to convert to JWK.", nameof(pem), ex);
            }
        }

        /// <summary>
        /// Parses a Base64-encoded JSON Web Key (JWK) string and returns a <see cref="PrivateJwk"/> object.
        /// </summary>
        /// <param name="base64">The Base64-encoded string representing a JWK. Must not be null or empty.</param>
        /// <returns>A <see cref="PrivateJwk"/> object initialized with the decoded JWK JSON string.</returns>
        /// <exception cref="ArgumentException">Thrown if <paramref name="base64"/> is null, empty, or not a valid Base64 string, or if the decoded string
        /// is not valid JWK JSON.</exception>
        public static PrivateJwk ParseFromBase64Encoded(string base64)
        {
            if (string.IsNullOrWhiteSpace(base64))
                throw new ArgumentException("JWK Base64 cannot be null or empty.", nameof(base64));

            string decoded;
            try
            {
                decoded = Encoding.UTF8.GetString(Convert.FromBase64String(base64));
            }
            catch (FormatException ex)
            {
                throw new ArgumentException("Invalid Base64 string.", ex);
            }

            // valider JSON
            try
            {
                JsonDocument.Parse(decoded);
            }
            catch (JsonException ex)
            {
                throw new ArgumentException("Decoded Base64 value is not valid JWK JSON.", ex);
            }

            return new PrivateJwk(decoded.Trim());
        }

        /// <inheritdoc/>
        public static implicit operator string(PrivateJwk jwk) => jwk._json;
    }

}
