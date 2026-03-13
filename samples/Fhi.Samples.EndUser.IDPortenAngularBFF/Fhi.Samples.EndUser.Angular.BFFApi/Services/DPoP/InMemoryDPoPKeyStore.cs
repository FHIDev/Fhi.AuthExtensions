using Fhi.Samples.EndUser.Angular.BFFApi.Services.Models;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;

namespace  Fhi.Samples.EndUser.Angular.BFFApi.Services.DPoP
{
    public class InMemoryDPoPKeyStore : IDPoPKeyStore
    {
        private readonly DpopKeyPair _keys;

        public InMemoryDPoPKeyStore()
        {
            using var rsa = RSA.Create(2048);

            var privateKey = new RsaSecurityKey(rsa.ExportParameters(true));
            var publicKey = JsonWebKeyConverter.ConvertFromRSASecurityKey(
                new RsaSecurityKey(rsa.ExportParameters(false))
            );

            _keys = new DpopKeyPair(privateKey, publicKey);
        }

        public Task<DpopKeyPair> GetKeyPairAsync() => Task.FromResult(_keys);
    }
}