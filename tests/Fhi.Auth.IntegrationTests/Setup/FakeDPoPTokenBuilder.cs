using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text;

namespace Fhi.Auth.IntegrationTests.Setup
{
    public static class FakeDPoPTokenBuilder
    {
        private static readonly RsaSecurityKey _rsaKey;
        private static readonly Dictionary<string, string> _publicKeyParams;
        private static readonly string _jwkThumbprint;

        public static RsaSecurityKey SecurityKey => _rsaKey;
        public static string JwkThumbprint => _jwkThumbprint;

        static FakeDPoPTokenBuilder()
        {
            var rsa = RSA.Create(2048);
            _rsaKey = new RsaSecurityKey(rsa);

            var parameters = rsa.ExportParameters(false);
            _publicKeyParams = new Dictionary<string, string>
            {
                ["kid"] = "kid",
                ["kty"] = "RSA",
                ["n"] = Base64UrlEncoder.Encode(parameters.Modulus!),
                ["e"] = Base64UrlEncoder.Encode(parameters.Exponent!),
            };

            var jwk = new JsonWebKey
            {
                Kid = "kid",
                Kty = "RSA",
                N = _publicKeyParams["n"],
                E = _publicKeyParams["e"],
            };
            _jwkThumbprint = Base64UrlEncoder.Encode(jwk.ComputeJwkThumbprint());
        }

        public static string CreateDPoPToken(
            string issuer,
            string audience,
            string alg = SecurityAlgorithms.RsaSha256)
        {
            return CreateDPoPToken(issuer, audience, JwkThumbprint, alg);
        }

        /// <summary>
        /// Creates a DPoP-bound access token.
        /// <para>
        /// <paramref name="jkt"/>: default (empty string) = use the builder's key thumbprint,
        /// null = omit the cnf claim entirely, any other value = use as jkt.
        /// </para>
        /// </summary>
        public static string CreateDPoPToken(
            string issuer,
            string audience,
            string jkt,
            string alg = SecurityAlgorithms.RsaSha256)
        {
            var signingCredentials = new SigningCredentials(_rsaKey, alg);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Issuer = issuer,
                Audience = audience,
                Expires = DateTime.UtcNow.AddHours(1),
                SigningCredentials = signingCredentials,
            };

            tokenDescriptor.Claims = new Dictionary<string, object>
            {
                ["cnf"] = new Dictionary<string, string> { ["jkt"] = jkt }
            };
            return new JwtSecurityTokenHandler().CreateEncodedJwt(tokenDescriptor);
        }

        /// <summary>
        /// Creates a DPoP proof JWT.
        /// <para>
        /// For <paramref name="jti"/>, <paramref name="htm"/>, <paramref name="htu"/>:
        /// empty string (default) = auto-compute, null = omit claim, any other value = use as-is.
        /// </para>
        /// <para>
        /// For <paramref name="iat"/>: <see cref="long.MinValue"/> (default) = DateTimeOffset.UtcNow,
        /// null = omit claim, any other value = use as Unix timestamp.
        /// </para>
        /// <para>
        /// For <paramref name="ath"/>: empty string (default) = auto-compute SHA-256 of accessToken,
        /// null = omit claim, any other value = use as-is.
        /// </para>
        /// <para>
        /// <paramref name="jwk"/>: null (default) = use the builder's public key, any other value = override.
        /// </para>
        /// </summary>
        public static string CreateDPoPProof(
            string url,
            string httpMethod,
            string accessToken,
            string? jti = "",
            string? htm = "",
            string? htu = "",
            long? iat = long.MinValue,
            string? ath = "",
            object? jwk = null,
            string typ = "dpop+jwt",
            SigningCredentials? signingCredentials = null,
            string alg = SecurityAlgorithms.RsaSha256)
        {
            signingCredentials ??= new SigningCredentials(_rsaKey, alg);

            var header = new JwtHeader(signingCredentials)
            {
                ["typ"] = typ,
                ["jwk"] = jwk ?? _publicKeyParams,
            };

            var payload = new JwtPayload();

            var resolvedJti = jti == "" ? Guid.NewGuid().ToString() : jti;
            var resolvedHtm = htm == "" ? httpMethod : htm;
            var resolvedHtu = htu == "" ? url : htu;
            var resolvedAth = ath == ""
                ? Base64UrlEncoder.Encode(SHA256.HashData(Encoding.UTF8.GetBytes(accessToken)))
                : ath;

            if (resolvedJti != null) payload["jti"] = resolvedJti;
            if (resolvedHtm != null) payload["htm"] = resolvedHtm;
            if (resolvedHtu != null) payload["htu"] = resolvedHtu;
            if (resolvedAth != null) payload["ath"] = resolvedAth;
            if (iat.HasValue)
                payload["iat"] = iat.Value == long.MinValue ? DateTimeOffset.UtcNow.ToUnixTimeSeconds() : iat.Value;

            return new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(header, payload));
        }

        /// <summary>
        /// Creates a DPoP proof where the jwk header contains the private key parameters,
        /// which should be rejected by the validator.
        /// </summary>
        public static string CreateDPoPProofWithPrivateKey(string url, string httpMethod, string accessToken)
        {
            var privateParams = _rsaKey.Rsa.ExportParameters(true);
            var jwkWithPrivateKey = new Dictionary<string, string>
            {
                ["kty"] = "RSA",
                ["n"] = Base64UrlEncoder.Encode(privateParams.Modulus!),
                ["e"] = Base64UrlEncoder.Encode(privateParams.Exponent!),
                ["d"] = Base64UrlEncoder.Encode(privateParams.D!),
                ["p"] = Base64UrlEncoder.Encode(privateParams.P!),
                ["q"] = Base64UrlEncoder.Encode(privateParams.Q!),
            };
            return CreateDPoPProof(url, httpMethod, accessToken, jwk: jwkWithPrivateKey);
        }

        /// <summary>
        /// Creates a DPoP proof where the signature is made with a different key than declared in the jwk header,
        /// simulating a tampered or forged proof.
        /// </summary>
        public static string CreateDPoPProofWithInvalidSignature(string url, string httpMethod, string accessToken)
        {
            var differentKey = new RsaSecurityKey(RSA.Create(2048));
            return CreateDPoPProof(url, httpMethod, accessToken,
                signingCredentials: new SigningCredentials(differentKey, SecurityAlgorithms.RsaSha256));
        }
    }
}
