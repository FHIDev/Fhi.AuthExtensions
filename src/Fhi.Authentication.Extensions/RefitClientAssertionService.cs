using Duende.AccessTokenManagement;
using Duende.IdentityModel;
using Duende.IdentityModel.Client;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using JwtClaimNames = System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames;

namespace Fhi.Authentication
{
    /// <summary>
    /// Client assertion service for Refit client credentials authentication using JWK-based private_key_jwt.
    /// </summary>
    internal class RefitClientAssertionService : IClientAssertionService
    {
        private readonly IDiscoveryCache _discoveryCache;
        private readonly string _clientId;
        private readonly string _jwk;

        public RefitClientAssertionService(IDiscoveryCache discoveryCache, string clientId, string jwk)
        {
            _discoveryCache = discoveryCache ?? throw new ArgumentNullException(nameof(discoveryCache));
            _clientId = clientId ?? throw new ArgumentNullException(nameof(clientId));
            _jwk = jwk ?? throw new ArgumentNullException(nameof(jwk));
        }

        public async Task<ClientAssertion?> GetClientAssertionAsync(string? clientName = null, TokenRequestParameters? parameters = null)
        {
            var discoveryDocument = await _discoveryCache.GetAsync();

            if (discoveryDocument.IsError)
            {
                throw new InvalidOperationException($"Failed to retrieve discovery document: {discoveryDocument.Error}");
            }

            // Create JWT token for client assertion using the JWK
            var jwt = CreateClientAssertionJwt(discoveryDocument.Issuer!, _clientId, _jwk);

            return new ClientAssertion
            {
                Type = OidcConstants.ClientAssertionTypes.JwtBearer,
                Value = jwt
            };
        }

        /// <summary>
        /// Creates a JWT client assertion token using the provided parameters.
        /// </summary>
        /// <param name="audience">The token audience (issuer from discovery document)</param>
        /// <param name="clientId">The OAuth 2.0 client identifier</param>
        /// <param name="privateKey">The JWK private key as JSON string</param>
        /// <returns>A signed JWT token for client assertion</returns>
        private static string CreateClientAssertionJwt(string audience, string clientId, string privateKey)
        {            // Create payload with required claims
            var claims = new List<Claim>
            {
                new Claim(JwtClaimNames.Sub, clientId),
                new Claim(JwtClaimNames.Iat, DateTimeOffset.Now.ToUnixTimeSeconds().ToString()),
                new Claim(JwtClaimNames.Jti, Guid.NewGuid().ToString("N")),
            };
            var payload = new JwtPayload(clientId, audience, claims, DateTime.UtcNow, DateTime.UtcNow.AddSeconds(60));

            // Create header with signing credentials
            var securityKey = new JsonWebKey(privateKey);
            if (string.IsNullOrEmpty(securityKey.Alg))
            {
                throw new InvalidOperationException("Missing algorithm in JWK. The 'alg' property is required for JWT signing.");
            }

            var signingCredentials = new SigningCredentials(securityKey, securityKey.Alg);
            var header = new JwtHeader(signingCredentials, null, "client-authentication+jwt");

            // Create and sign the JWT
            var jwtSecurityToken = new JwtSecurityToken(header, payload);
            return new JwtSecurityTokenHandler().WriteToken(jwtSecurityToken);
        }
    }
}
