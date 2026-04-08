using Fhi.Authentication.Tokens;
using Fhi.Security.Cryptography.Jwks;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Fhi.Authentication.Extensions.UnitTests.Tokens
{
    public class ClientAssertionTokenHandlerTests
    {
        /// <summary>
        /// Generate private and public key using JwkGenerator and create a client assertion token from private key using DefaultClientAssertionTokenHandler.
        /// </summary>
        [Test]
        public void GenerateKeysAndClientAssertion()
        {
            var keys = JWK.Create();

            var assertion = ClientAssertionTokenHandler.CreateJwtToken("http://issuer", "clientId", keys.PrivateKey);

            var handler = new JsonWebTokenHandler();
            var token = handler.ReadJsonWebToken(assertion);

            Assert.Multiple(() =>
            {
                Assert.That(token.Issuer, Is.EqualTo("clientId"), "Issuer mismatch");

                Assert.That(token.GetClaim("aud").Value, Is.EqualTo("http://issuer"), "Invalid 'aud' claim value");
                Assert.That(token.GetClaim("sub").Value, Is.EqualTo("clientId"), "Invalid 'sub' claim value");
                Assert.That(token.TryGetClaim("jti", out _), Is.True, "Missing 'jti' claim");
                Assert.That(token.TryGetClaim("nbf", out _), Is.True, "Missing 'nbf' claim");
                Assert.That(token.TryGetClaim("iat", out _), Is.True, "Missing 'iat' claim");
                Assert.That(token.TryGetClaim("exp", out _), Is.True, "Missing 'exp' claim");

                Assert.That(token.Typ, Is.EqualTo("client-authentication+jwt"));
                Assert.That(token.Alg, Is.EqualTo(SecurityAlgorithms.RsaSha512));

                var jwk = new JsonWebKey(keys.PrivateKey);
                Assert.That(token.Kid, Is.EqualTo(jwk.Kid));
            });
        }

        [Test]
        public async Task CreateJwtToken_ShouldProduceValidSignature()
        {
            var keys = JWK.Create();

            var assertion = ClientAssertionTokenHandler.CreateJwtToken(
                "http://issuer", "clientId", keys.PrivateKey);

            var handler = new JsonWebTokenHandler();
            var validationResult = await handler.ValidateTokenAsync(assertion,
                new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = false,
                    IssuerSigningKey = new JsonWebKey(keys.PublicKey),
                });

            Assert.That(validationResult.IsValid, Is.True);
        }

        /// <summary>
        /// Verify that custom expiration time is correctly applied to the client assertion token.
        /// </summary>
        [Test]
        public void ClientAssertion_WithCustomExpiration_ShouldSetCorrectExpirationTime()
        {
            var customExpiration = DateTime.UtcNow.AddSeconds(30);

            var assertion = ClientAssertionTokenHandler.CreateJwtToken(
                "http://issuer",
                "clientId",
                JWK.Create().PrivateKey,
                customExpiration);

            var handler = new JsonWebTokenHandler();
            var token = handler.ReadJsonWebToken(assertion);

            Assert.That(token.ValidTo.Second, Is.EqualTo(customExpiration.Second));
        }
    }
}
