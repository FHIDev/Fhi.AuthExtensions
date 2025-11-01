using Duende.IdentityModel.Client;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;

namespace Fhi.Samples.M2M.Manuel
{
    /// <summary>
    /// Sample requests to Maskinporten
    /// </summary>
    public class MaskinPortenSampleRequests
    {
        /// <summary>
        /// Sample requests to Maskinporten using a PEM formatted private key.
        /// </summary>
        /// <returns></returns>
        [Test]
        public async Task MaskinportenTokenRequest_WithPEMKeyFormat()
        {
            string pem = """
                -----"BEGIN PRIVATE KEY-----
                -----END PRIVATE KEY-----
                """;
            var kid = "";
            var scope = "fhi:authextensionssample.access";
            var clientId = "02cf7f3e-a5bb-408f-a00d-d341b58a9962";
            var clientAssertionAudience = "https://test.maskinporten.no/";

            var client = new HttpClient();
            var response = await client.RequestTokenRawAsync("https://test.maskinporten.no/token", new Parameters
            {
                { "grant_type", "urn:ietf:params:oauth:grant-type:jwt-bearer" },
                { "assertion",  GenerateClientAssertionFromPem(pem, kid, clientAssertionAudience, clientId, scope) }
            });
        }

        /// <summary>
        /// Sample requests to Maskinporten using a JWK formatted private key.
        /// </summary>
        /// <returns></returns>
        [Test]
        public async Task MaskinportenTokenRequest_WithJWKKeyFormat()
        {
            string jwk = """

                """;
            var kid = "";
            var scope = "fhi:authextensionssample.access";
            var clientId = "02cf7f3e-a5bb-408f-a00d-d341b58a9962";
            var clientAssertionAudience = "https://test.maskinporten.no/";

            var client = new HttpClient();
            var response = await client.RequestTokenRawAsync("https://test.maskinporten.no/token", new Parameters
            {
                { "grant_type", "urn:ietf:params:oauth:grant-type:jwt-bearer" },
                { "assertion",  GetJwtAssertion(jwk,kid, clientAssertionAudience, clientId, scope) }
            });
        }

        /// <summary>
        /// Generates a client assertion JWT using the provided PEM-encoded private key.
        /// </summary>
        /// <remarks>The generated JWT includes standard claims such as "aud" (audience), "iss" (issuer),
        /// "exp" (expiration time), "iat" (issued at time), and "jti" (JWT ID). The token is signed using RSA
        /// SHA-256.</remarks>
        /// <param name="pem">The PEM-encoded RSA private key used to sign the JWT.</param>
        /// <param name="kid">The key identifier (KID) for the RSA security key.</param>
        /// <param name="audience">The audience claim that identifies the recipients that the JWT is intended for.</param>
        /// <param name="issuer">The issuer claim is the ClientId.</param>
        /// <param name="scope">The scope claim that specifies the permissions granted by the JWT.</param>
        /// <returns>A JWT string representing the client assertion.</returns>
        public static string GenerateClientAssertionFromPem(string pem, string kid, string audience, string issuer, string scope)
        {
            var rsa = RSA.Create();
            rsa.ImportFromPem(pem);

            var header = new JwtHeader(
                new SigningCredentials(new RsaSecurityKey(rsa) { KeyId = kid },
                SecurityAlgorithms.RsaSha256), null, "jwt");

            long issuedAt = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();
            long expirationTime = issuedAt + 120;
            var body = new JwtPayload
            {
                { "aud", audience },
                { "scope", scope },
                { "iss", issuer },
                { "exp", expirationTime },
                { "iat", issuedAt },
                { "jti", Guid.NewGuid().ToString() },
            };

            var jwtSecurityToken = new JwtSecurityToken(header, body);
            return new JwtSecurityTokenHandler().WriteToken(jwtSecurityToken);
        }

        private string GetJwtAssertion(string jwk, string kid, string audience, string issuer, string scope)
        {
            var securityKey = new JsonWebKey(jwk);
            JwtHeader header = new JwtHeader(
                new SigningCredentials(securityKey, SecurityAlgorithms.RsaSha256));

            long issuedAt = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();
            var securityToken = new JwtSecurityToken(header,
                new JwtPayload
                {
                    { "aud", audience },
                    { "scope", scope },
                    { "iss", issuer },
                    { "exp", issuedAt + 120 },
                    { "iat", issuedAt },
                    { "jti", Guid.NewGuid().ToString() },
                });

            var tokenHandler = new JwtSecurityTokenHandler();
            return tokenHandler.WriteToken(securityToken);
        }
    }
}
