using Duende.IdentityModel;
using Duende.IdentityModel.Client;
using Fhi.Authentication.Tokens;
using Microsoft.Extensions.Logging;

namespace Fhi.Authentication.OpenIdConnect
{
    internal record TokenResponse(string? AccessToken, bool IsError, string? ErrorDescription);
    internal interface ITokenService
    {
        /// <summary>
        /// Create DPoP token
        /// </summary>
        /// <param name="authority">The OpenId connectprovider authority Url</param>
        /// <param name="clientId">Client Identifier</param>
        /// <param name="jwk">The private json web key for client assertion</param>
        /// <param name="scopes">Separated list of scopes</param>
        /// <param name="dPopJwk">The private json web key for DPoP</param>
        /// <returns></returns>
        public Task<TokenResponse> RequestDPoPToken(string authority, string clientId, string jwk, string scopes, string dPopJwk);
    }

    internal class TokenService(
        ILogger<TokenService> Logger,
        IHttpClientFactory HttpClientFactory) : ITokenService
    {
        public async Task<TokenResponse> RequestDPoPToken(
            string authority,
            string clientId,
            string jwk,
            string scopes,
            string dPopJwk)
        {
            var client = HttpClientFactory.CreateClient();
            Logger.LogInformation("Get metadata from discovery endpoint from Authority {@Authority}", authority);
            var discovery = await client.GetDiscoveryDocumentAsync(authority);
            if (discovery is not null && !discovery.IsError && discovery.Issuer is not null && discovery.TokenEndpoint is not null)
            {
                var response = await client.RequestTokenWithDPoP(discovery, clientId, jwk, scopes, dPopJwk);

                if (response.IsError &&
                    response.Error == "use_dpop_nonce" &&
                    response.HttpResponse?.Headers.Contains("DPoP-Nonce") == true)
                {
                    var nonce = response.HttpResponse.Headers.GetValues("DPoP-Nonce").FirstOrDefault();
                    if (!string.IsNullOrEmpty(nonce))
                    {
                        response = await client.RequestTokenWithDPoP(discovery, clientId, jwk, scopes, dPopJwk, nonce);
                    }
                }

                return response.IsError ?
                    new TokenResponse(null, true, response.ErrorDescription) :
                    new TokenResponse(response.AccessToken, false, string.Empty);
            }

            return new TokenResponse(null, true, discovery is null ? "No discovery document" : discovery.Error);
        }
    }

    internal static class HttpClientExtensions
    {
        internal static async Task<Duende.IdentityModel.Client.TokenResponse> RequestTokenWithDPoP(
          this HttpClient client,
          DiscoveryDocumentResponse discovery,
          string clientId,
          string jwk,
          string scopes,
          string dPopJwk,
          string? nonce = null)
        {
            var tokenRequest = new ClientCredentialsTokenRequest
            {
                ClientId = clientId,
                Address = discovery.TokenEndpoint,
                GrantType = OidcConstants.GrantTypes.ClientCredentials,
                ClientCredentialStyle = ClientCredentialStyle.PostBody,
                DPoPProofToken = DPoPProofGenerator.CreateDPoPProof(
                    discovery.TokenEndpoint!,
                    "POST",
                    dPopJwk,
                    "PS256",
                    dPoPNonce: nonce),
                ClientAssertion = new ClientAssertion
                {
                    Type = OidcConstants.ClientAssertionTypes.JwtBearer,
                    Value = ClientAssertionTokenHandler.CreateJwtToken(discovery.Issuer!, clientId, jwk)
                },
                Scope = scopes
            };

            return await client.RequestClientCredentialsTokenAsync(tokenRequest);
        }
    }
}