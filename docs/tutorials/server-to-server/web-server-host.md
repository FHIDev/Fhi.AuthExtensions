# Client credentials token request from a Web host

In ASP.NET Core applications, you can securely call downstream APIs using `IHttpClientFactory` together with Duende's `AccessTokenRequestHandler`. This approach ensures that access tokens are automatically managed and refreshed when needed.

## Code Sample
See the code lab for a working example: [Call API from Web server host using IHttpClientFactory and Duende AccessTokenManagement](../../code-lab/client-credentials/webserver-host-sample.ipynb)

## Flow Description
The diagrams below show how an `HttpClient` created by `IHttpClientFactory` uses Duende's token management to attach and refresh access tokens when calling a protected API.

- *The simplified diagram* shows the main steps token retrieval, cache/expiration check, and API call.
- *The detailed diagram* breaks down the internal calls between Duende components.

**Simplified API call sequence with Duende.AccessTokenManagement**

```mermaid
sequenceDiagram
    participant TestService as Service
    participant HttpClientFactory
    participant Duende.AccessTokenManagement
    participant TokenEndpoint as OIDC provider
    participant API as API

    TestService->>HttpClientFactory: CreateClient("m2m")
    Note left of Duende.AccessTokenManagement: Duende has an Http Delegation handler that <br> adds authorization header to the request, AccessTokenRequestHandler
    HttpClientFactory->>Duende.AccessTokenManagement: SendAsync(request)
    
    Note left of Duende.AccessTokenManagement: GetToken from cache and check expiration. 
    alt Token expired or empty
        Note left of Duende.AccessTokenManagement: call configured token endpoint to get a new token with clientId and secret.
        Duende.AccessTokenManagement->>TokenEndpoint: POST /connect/token
        TokenEndpoint-->>Duende.AccessTokenManagement: Access token
        Duende.AccessTokenManagement-->>Duende.AccessTokenManagement: Update cahce with new token and add header
    else Token valid
        Duende.AccessTokenManagement-->>Duende.AccessTokenManagement: Add existing token to header
    end
    Note left of Duende.AccessTokenManagement: For DPoP DPoP header will also be added. 
    Duende.AccessTokenManagement-->>HttpClientFactory: Attach token to Authorization header
    HttpClientFactory-->>TestService: Configured HttpClient

    TestService->>API: GET /api/v1/integration/health-records\n(with Bearer or DPoP token)
    API-->>TestService: Response

```

**Detailed API call sequence with Duende.AccessTokenManagement**

```mermaid
sequenceDiagram
    participant TestService
    participant HttpClientFactory
    participant AccessTokenRequestHandler as Duende.AccessTokenRequestHandler
    participant ITokenRetriever as Duende.ITokenRetriever
    participant IClientCredentialsTokenManager as Duende.IClientCredentialsTokenManager
    participant TokenEndpoint as IdentityServer
    participant API as Protected API

    TestService->>HttpClientFactory: CreateClient("m2m")
    HttpClientFactory->>AccessTokenRequestHandler: SendAsync(request)
    AccessTokenRequestHandler->>ITokenRetriever: Get token
    ITokenRetriever->>IClientCredentialsTokenManager: GetToken
    Note right of IClientCredentialsTokenManager: GetToken checks cache and expiry
    IClientCredentialsTokenManager->>IClientCredentialsTokenManager: Is token expired?
    alt Token expired
        IClientCredentialsTokenManager->>TokenEndpoint: POST /connect/token
        TokenEndpoint-->>IClientCredentialsTokenManager: Access token
        IClientCredentialsTokenManager-->>ITokenRetriever: New token
    else Token valid
        IClientCredentialsTokenManager-->>ITokenRetriever: Existing token
    end
    ITokenRetriever-->>AccessTokenRequestHandler: Token
    AccessTokenRequestHandler-->>HttpClientFactory: Attach token to Authorization header
    HttpClientFactory-->>TestService: Configured HttpClient

    TestService->>API: GET /api/v1/integration/health-records\n(with Bearer token)
    API-->>TestService: Response

```
