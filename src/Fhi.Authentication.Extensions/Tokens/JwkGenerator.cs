using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.IdentityModel.Tokens;

namespace Fhi.Authentication.Tokens
{
    /// <summary>
    /// Generate Json Web Keys
    /// </summary>
    /// <param name="PublicKey">Public key</param>
    /// <param name="PrivateKey">Public and private key</param>
    public record JwkKeyPair(string PublicKey, string PrivateKey);

    /// <summary>
    /// Generate Json Web Keys used for client assertion and DPoP
    /// </summary>
    public static class JwkGenerator
    {
        /// <summary>
        /// Generate a Json web key with RSA signing algorithm. Returns both private key and public key 
        /// Following key requirement https://utviklerportal.nhn.no/informasjonstjenester/helseid/protokoller-og-sikkerhetsprofil/sikkerhetsprofil/docs/vedlegg/krav_til_kryptografi_enmd
        /// </summary>
        /// <param name="signingAlgorithm"></param>
        /// <param name="keyUse"></param>
        /// <param name="kid"></param>
        /// <returns></returns>
        public static JwkKeyPair GenerateRsaJwk(
            string signingAlgorithm = SecurityAlgorithms.RsaSha512,
            string keyUse = "sig",
            string? kid = null)
        {
            using var rsa = RSA.Create(4096);
            var rsaParameters = rsa.ExportParameters(true);

            var privateJwk = new JsonWebKey
            {
                Alg = signingAlgorithm,
                Kty = "RSA",
                N = Base64UrlEncoder.Encode(rsaParameters.Modulus),
                E = Base64UrlEncoder.Encode(rsaParameters.Exponent),
                D = Base64UrlEncoder.Encode(rsaParameters.D),
                P = Base64UrlEncoder.Encode(rsaParameters.P),
                Q = Base64UrlEncoder.Encode(rsaParameters.Q),
                DP = Base64UrlEncoder.Encode(rsaParameters.DP),
                DQ = Base64UrlEncoder.Encode(rsaParameters.DQ),
                QI = Base64UrlEncoder.Encode(rsaParameters.InverseQ),
                Use = keyUse,
            };

            if (string.IsNullOrWhiteSpace(kid))
            {
                privateJwk.Kid = Base64UrlEncoder.Encode(privateJwk.ComputeJwkThumbprint());
            }
            else
            {
                privateJwk.Kid = kid;
            }

            var publicJwk = new JsonWebKey
            {
                Alg = signingAlgorithm,
                Kty = privateJwk.Kty,
                Kid = privateJwk.Kid,
                N = privateJwk.N,
                E = privateJwk.E,
                Use = keyUse
            };

            string privateJwkJson = JsonSerializer.Serialize(privateJwk);
            string publicJwkJson = publicJwk.ToPublicJwk();

            return new JwkKeyPair(publicJwkJson, privateJwkJson);
        }
    }
}
