using System.Text.Json;
using Fhi.Authentication.Tokens;
using Microsoft.IdentityModel.Tokens;

namespace Fhi.Authentication.UnitTests.Tokens
{
    public class JwkGeneratorTests
    {
        /// <summary>
        /// Generate JWK with default parameters and validate key structure.
        /// </summary>
        [Test]
        public void GIVEN_GenerateJwk_WHEN_OnlyDefaultValues_THEN_CreateValidKeyPair()
        {
            var keys = JwkGenerator.GenerateRsaJwk();

            var publicJwk = JsonSerializer.Deserialize<JsonWebKey>(keys.PublicKey);
            var privateJwk = JsonSerializer.Deserialize<JsonWebKey>(keys.PrivateKey);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(keys.PublicKey, Is.Not.Empty, "Missing Public Key.");
                Assert.That(keys.PrivateKey, Is.Not.Empty, "Missing Private Key.");

                Assert.That(publicJwk?.Kty, Is.EqualTo("RSA"), "Wrong/ missing kty.");
                Assert.That(publicJwk?.Use, Is.EqualTo("sig"), "Wrong/ missing use.");
                Assert.That(publicJwk?.Kid, Is.EqualTo(privateJwk?.Kid), "Kid does not match.");
                Assert.That(publicJwk?.N, Is.EqualTo(privateJwk?.N), "N does not match.");
                Assert.That(publicJwk?.E, Is.EqualTo(privateJwk?.E), "E does not match.");
            }
        }

        /// <summary>
        /// Generate JWK with custom kid and verify it is applied.
        /// </summary>
        [Test]
        public void GIVEN_GenerateJwk_WHEN_CustomKidValue_THEN_CreateValidKeyPairWithCustomKid()
        {
            var customKid = "custom-key-id";
            var keys = JwkGenerator.GenerateRsaJwk(kid: customKid);

            var publicJwk = JsonSerializer.Deserialize<JsonWebKey>(keys.PublicKey);
            var privateJwk = JsonSerializer.Deserialize<JsonWebKey>(keys.PrivateKey);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(publicJwk?.Kid, Is.EqualTo(customKid), "Incorrect kid.");
                Assert.That(privateJwk?.Kid, Is.EqualTo(customKid), "Incorrect kid.");
            }
        }

        /// <summary>
        /// Generate JWK with invalid key use and expect exception.
        /// </summary>
        [Test]
        public void GIVEN_GenerateJwk_WHEN_InvalidKeyUseType_THEN_ThrowException()
        {
            var ex = Assert.Throws<ArgumentException>(() =>
            {
                JwkGenerator.GenerateRsaJwk(keyUse: "invalid");
            });

            Assert.That(ex.Message, Does.Contain("Invalid key use"), "Wrong or missing exception.");
        }

        /// <summary>
        /// Generate JWK with invalid signing algorithm and expect exception.
        /// </summary>
        [Test]
        public void GIVEN_GenerateJwk_WHEN_InvalidSigningAlgorithm_THEN_ThrowException()
        {
            var ex = Assert.Throws<ArgumentException>(() =>
            {
                JwkGenerator.GenerateRsaJwk(signingAlgorithm: "none");
            });

            Assert.That(ex.Message, Does.Contain("Invalid signing algorithm"), "Wrong or missing exception.");
        }
    }
}