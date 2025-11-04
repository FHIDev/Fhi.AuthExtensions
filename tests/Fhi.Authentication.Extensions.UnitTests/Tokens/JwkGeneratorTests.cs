using Fhi.Authentication.Tokens;

namespace Fhi.Authentication.Extensions.UnitTests.Tokens
{
    public class JwkGeneratorTests
    {
        [Test]
        public void GenerateJwk()
        {
            var jwk = JwkGenerator.GenerateRsaJwk();
            Console.WriteLine(jwk);
        }
    }
}
