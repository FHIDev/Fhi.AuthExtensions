using Fhi.Authentication.Tokens;

namespace Fhi.Authentication.Extensions.UnitTests.Tokens
{
    public class JwkGeneratorTests
    {
        [Test]
        public void GIVEN_GenerateJwk_WHEN_OnlyDefaultValues_THEN_CreateValidKeyPair()
        {
            var jwk = JwkGenerator.GenerateRsaJwk();
            Console.WriteLine(jwk);
        }

        [Test]
        public void GIVEN_GenerateJwk_WHEN_CustomKidValue_THEN_CreateValidKeyPairWithCustomKid()
        {
            var jwk = JwkGenerator.GenerateRsaJwk();
            Console.WriteLine(jwk);
        }

        [Test]
        public void GIVEN_GenerateJwk_WHEN_InvalidKeyUseType_THEN_ReturnEmptyKeyPair()
        {
            var jwk = JwkGenerator.GenerateRsaJwk();
            Console.WriteLine(jwk);
        }

        [Test]
        public void GIVEN_GenerateJwk_WHEN_InvalidSigningAlgorithm_THEN_ReturnEmptyKeyPair()
        {
            var jwk = JwkGenerator.GenerateRsaJwk();
            Console.WriteLine(jwk);
        }
    }
}
