using Duende.IdentityModel;
using Duende.IdentityModel.Client;
using Fhi.Authentication.Tokens;

namespace Fhi.Authentication.Setup;

public static class HttpClientExtensions
{
    public static async Task<TokenResponse> RequestTokenWithDPoP(
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