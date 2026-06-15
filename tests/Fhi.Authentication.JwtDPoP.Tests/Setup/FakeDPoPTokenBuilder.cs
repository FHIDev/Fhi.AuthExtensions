using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text;

namespace Fhi.Authentication.JwtDPoP.Tests.Setup
{
    public static class FakeDPoPTokenBuilder
    {
        public class FakeDpopKeyPair
        {
            public SecurityKey? PrivateKey { get; init; }
            public IDictionary<string, string>? PublicJwk { get; init; }
            public string? JwkThumbprint { get; init; }
        }

        public static SecurityKey SecurityKey => _keyPair.PrivateKey!;

        public static string JwkThumbprint => _keyPair.JwkThumbprint!;

        public static class FakeDPoPKeys
        {
            public static FakeDpopKeyPair CreateRsa()
            {
                var rsa = RSA.Create(2048);
                var key = new RsaSecurityKey(rsa);

                var p = rsa.ExportParameters(false);

                var jwk = new Dictionary<string, string>
                {
                    ["kty"] = "RSA",
                    ["kid"] = "kid",
                    ["n"] = Base64UrlEncoder.Encode(p.Modulus!),
                    ["e"] = Base64UrlEncoder.Encode(p.Exponent!)
                };

                var thumb = new JsonWebKey
                {
                    Kty = "RSA",
                    Kid = "kid",
                    N = jwk["n"],
                    E = jwk["e"]
                }.ComputeJwkThumbprint();

                return new FakeDpopKeyPair
                {
                    PrivateKey = key,
                    PublicJwk = jwk,
                    JwkThumbprint = Base64UrlEncoder.Encode(thumb)
                };
            }

            public static FakeDpopKeyPair CreateEc(ECCurve curve, string kty)
            {
                var ec = ECDsa.Create(curve);
                var key = new ECDsaSecurityKey(ec);

                var p = ec.ExportParameters(false);

                string crv = curve.Oid.FriendlyName switch
                {
                    "nistP256" => "P-256",
                    "nistP384" => "P-384",
                    "nistP521" => "P-521",
                    _ => throw new NotSupportedException($"Unsupported curve: {curve.Oid.FriendlyName}")
                };

                var jwk = new Dictionary<string, string>
                {
                    ["kty"] = kty,
                    ["kid"] = "kid",
                    ["crv"] = crv,
                    ["x"] = Base64UrlEncoder.Encode(p.Q.X!),
                    ["y"] = Base64UrlEncoder.Encode(p.Q.Y!)
                };

                var thumb = new JsonWebKey
                {
                    Kty = kty,
                    Kid = "kid",
                    Crv = crv,
                    X = jwk["x"],
                    Y = jwk["y"]
                }.ComputeJwkThumbprint();

                return new FakeDpopKeyPair
                {
                    PrivateKey = key,
                    PublicJwk = jwk,
                    JwkThumbprint = Base64UrlEncoder.Encode(thumb)
                };
            }
        }

        private static FakeDpopKeyPair _keyPair = FakeDPoPKeys.CreateRsa();

        public static void UseKey(FakeDpopKeyPair pair)
        {
            _keyPair = pair;
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
            var signingCredentials = new SigningCredentials(_keyPair.PrivateKey, alg);

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
            signingCredentials ??= new SigningCredentials(_keyPair.PrivateKey, alg);

            var header = new JwtHeader(signingCredentials)
            {
                ["typ"] = typ,
                ["jwk"] = jwk ?? _keyPair.PublicJwk
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
            if (_keyPair.PrivateKey is not RsaSecurityKey rsaKey)
                throw new InvalidOperationException("Current key is not RSA");

            var p = rsaKey.Rsa.ExportParameters(true);

            var jwkWithPrivate = new Dictionary<string, string>
            {
                ["kty"] = "RSA",
                ["n"] = Base64UrlEncoder.Encode(p.Modulus!),
                ["e"] = Base64UrlEncoder.Encode(p.Exponent!),
                ["d"] = Base64UrlEncoder.Encode(p.D!),
                ["p"] = Base64UrlEncoder.Encode(p.P!),
                ["q"] = Base64UrlEncoder.Encode(p.Q!)
            };

            return CreateDPoPProof(url, httpMethod, accessToken, jwk: jwkWithPrivate);
        }

        public static string CreateDPoPProofWithPrivateEcKey(string url, string httpMethod, string accessToken)
        {
            if (_keyPair.PrivateKey is not ECDsaSecurityKey ecKey)
                throw new InvalidOperationException("Current key is not EC");

            var p = ecKey.ECDsa.ExportParameters(true);

            var jwkWithPrivate = new Dictionary<string, string>
            {
                ["kty"] = "EC",
                ["crv"] = ecKey.ECDsa.ExportParameters(false).Curve.Oid.FriendlyName!,
                ["x"] = Base64UrlEncoder.Encode(p.Q.X!),
                ["y"] = Base64UrlEncoder.Encode(p.Q.Y!),
                ["d"] = Base64UrlEncoder.Encode(p.D!)
            };

            return CreateDPoPProof(url, httpMethod, accessToken, jwk: jwkWithPrivate);
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
