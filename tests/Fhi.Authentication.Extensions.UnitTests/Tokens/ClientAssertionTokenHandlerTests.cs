using System.IdentityModel.Tokens.Jwt;
using Fhi.Authentication.Tokens;
using Fhi.Security.Cryptography;
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

            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(assertion);

            Assert.Multiple(() =>
            {
                Assert.That(token.Issuer, Is.EqualTo("clientId"), "Issuer mismatch");
                Assert.That(token.Claims.Count, Is.EqualTo(7), "Unexpected claim count");

                var audClaim = token.Claims.SingleOrDefault(x => x.Type == "aud");
                Assert.That(audClaim, Is.Not.Null, "Missing 'aud' claim");
                Assert.That(audClaim!.Value, Is.EqualTo("http://issuer"), "Invalid 'aud' claim value");

                var subClaim = token.Claims.SingleOrDefault(x => x.Type == "sub");
                Assert.That(subClaim, Is.Not.Null, "Missing 'sub' claim");
                Assert.That(subClaim!.Value, Is.EqualTo("clientId"), "Invalid 'sub' claim value");

                Assert.That(token.Claims.Any(x => x.Type == "jti"), Is.True, "Missing 'jit' claim");
                Assert.That(token.Claims.Any(x => x.Type == "nbf"), Is.True, "Missing 'nbf' claim");
                Assert.That(token.Claims.Any(x => x.Type == "iat"), Is.True, "Missing 'iat' claim");
                Assert.That(token.Claims.Any(x => x.Type == "exp"), Is.True, "Missing 'exp' claim");

                var typ = token.Header.SingleOrDefault(x => x.Key == "typ");
                Assert.That(typ.Value, Is.EqualTo("client-authentication+jwt"));

                var alg = token.Header.SingleOrDefault(x => x.Key == "alg");
                Assert.That(alg.Value, Is.EqualTo(SecurityAlgorithms.RsaSha512));

                var jwk = new JsonWebKey(keys.PrivateKey);
                var kid = token.Header.SingleOrDefault(x => x.Key == "kid");
                Assert.That(kid.Value, Is.EqualTo(jwk.Kid));
            });
        }

        /// <summary>
        /// Verify that custom expiration time is correctly applied to the client assertion token.
        /// </summary>
        [Test]
        public void ClientAssertion_WithCustomExpiration_ShouldSetCorrectExpirationTime()
        {
            var keys = JWK.Create();
            var customExpiration = DateTime.UtcNow.AddSeconds(30);

            var assertion = ClientAssertionTokenHandler.CreateJwtToken("http://issuer", "clientId", keys.PrivateKey, customExpiration);

            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(assertion);

            // The token.ValidTo property returns the expiration as a DateTime in UTC
            var actualExpiration = token.ValidTo;
            
            // Allow 1 second tolerance for test execution time and any rounding differences
            var timeDifference = Math.Abs((actualExpiration - customExpiration).TotalSeconds);
            Assert.That(timeDifference, Is.LessThanOrEqualTo(1), 
                $"Expiration time mismatch. Expected: {customExpiration:O}, Actual: {actualExpiration:O}");
        }
    }
}