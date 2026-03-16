using Fhi.Samples.EndUser.Angular.BFFApi.Services.Models;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

namespace Fhi.Samples.EndUser.Angular.BFFApi.Services.DPoP
{
    /// <summary>
    /// Generates a DPoP proof JWT according to the OAuth DPoP specification.
    /// A DPoP proof is a signed JWT that binds:
    /// - the HTTP method (htm)
    /// - the HTTP URL (htu)
    /// - a timestamp (iat)
    /// - a unique identifier (jti)
    /// to a public key (embedded as a JWK in the header).
    ///
    /// The authorization server uses this proof to bind issued tokens to the
    /// client's public key, preventing token replay and theft.
    /// </summary>
    public class DPoPProofGenerator : IDPoPProofGenerator
    {
        /// <summary>
        /// Creates a DPoP proof JWT for a specific HTTP request.
        /// The proof must be included in the "DPoP" header when calling
        /// the token endpoint or a protected resource.
        ///
        /// Required claims:
        /// - htm: HTTP method (e.g., "GET", "POST")
        /// - htu: Full request URL (scheme + host + path)
        /// - iat: Issued-at timestamp (seconds since epoch)
        /// - jti: Unique identifier (UUID)
        ///
        /// Required header fields:
        /// - typ: "dpop+jwt"
        /// - alg: Signing algorithm (RS256)
        /// - jwk: Public key used to verify the signature
        /// </summary>
        public Task<string> CreateProofAsync(string method, string url, DpopKeyPair keyPair)
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            // Payload claims required by the DPoP specification
            var payload = new JwtPayload
            {
                { "htu", url },                       // HTTP URI of the request
                { "htm", method.ToUpperInvariant() }, // HTTP method
                { "iat", now },                       // Issued-at timestamp
                { "jti", Guid.NewGuid().ToString() }  // Unique identifier
            };

            // Header with signing algorithm and embedded public JWK
            var header = new JwtHeader(
                new SigningCredentials(keyPair.PrivateKey, SecurityAlgorithms.RsaSha256)
            );

            // Required by DPoP: typ must be "dpop+jwt"
            header["typ"] = "dpop+jwt";

            // Required: include the public JWK so the server can verify the signature
            header["jwk"] = keyPair.PublicJwk;

            // Create and serialize the JWT
            var token = new JwtSecurityToken(header, payload);
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.WriteToken(token);

            return Task.FromResult(jwt);
        }
    }
}