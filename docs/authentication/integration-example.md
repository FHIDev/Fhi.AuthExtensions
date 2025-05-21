# Integration Example: Refit Client Credentials → WebApi

This document explains the integration between the Refit Client Credentials sample and the WebApi sample in this solution.

## Architecture Overview

```mermaid
graph LR
    A[Refit Client] --> B[Token Manager]
    B --> C[HelseID OAuth Server]
    A --> D[WebApi Health Records]
    C --> D
    
    subgraph "OAuth Flow"
        B --> |"client_credentials"| C
        C --> |"access_token"| B
    end
    
    subgraph "API Call"
        A --> |"GET /health-records"| D
        D --> |"Bearer token validation"| C
        D --> |"JSON response"| A
    end
```

## Components

### 1. **Refit Client Credentials Sample** (`Fhi.Samples.RefitClientCredentials`)

**Purpose**: Demonstrates OAuth 2.0 client credentials flow with Refit

**Key Files**:
- `IHealthRecordsApi.cs` - Refit interface matching WebApi endpoints
- `HealthRecordsWorker.cs` - Background service that calls the API
- `Program.cs` - Service registration using `AddRefitClientWithClientCredentials`

**Configuration**:
```json
{
  "RefitClientCredentials": {
    "ClientName": "HealthRecordsApiClient",
    "TokenEndpoint": "https://helseid-sts.test.nhn.no/connect/token",
    "ClientId": "your-client-id",
    "ClientSecret": "your-client-secret",
    "Scope": "fhi:webapi/health-records.read",
    "ApiBaseUrl": "https://localhost:7150"
  }
}
```

### 2. **WebApi Sample** (`Fhi.Samples.WebApi`)

**Purpose**: Protected API that requires OAuth 2.0 authentication

**Key Files**:
- `HealthRecordController.cs` - Integration endpoint requiring authentication
- `HostingExtensions.cs` - Authentication and authorization configuration

**Security Configuration**:
- **Authentication Scheme**: `bearer.integration`
- **Required Scope**: `fhi:webapi/health-records.read`
- **Authority**: `https://helseid-sts.test.nhn.no/`

## Integration Flow

### 1. **Service Registration** (Client)
```csharp
builder.Services.AddRefitClientWithClientCredentials<IHealthRecordsApi>(builder.Configuration);
```

This registers:
- Duende AccessTokenManagement for client credentials
- Refit client with automatic token attachment
- HTTP client with base address and delegating handlers

### 2. **Token Acquisition** (Automatic)
When the Refit client makes a request:
1. **Token Manager** checks for valid cached token
2. If expired/missing, requests new token from HelseID
3. **OAuth Request**: `POST /connect/token` with `client_credentials` grant
4. **Token Response**: Access token with required scope
5. **Token Caching**: Stores token until expiration

### 3. **API Call** (Client → WebApi)
```http
GET /api/v1/integration/health-records HTTP/1.1
Host: localhost:7150
Authorization: Bearer eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiJ9...
```

### 4. **Token Validation** (WebApi)
1. **Extract Bearer Token** from Authorization header
2. **Validate with HelseID** (signature, issuer, audience, expiration)
3. **Check Required Scope**: `fhi:webapi/health-records.read`
4. **Authorization Policy**: `Integration` policy with `bearer.integration` scheme

### 5. **Response** (WebApi → Client)
```json
[
  {
    "name": "Sample Record 1",
    "description": "Sample health record description",
    "createdAt": "2025-06-01T08:00:00Z"
  }
]
```

## Security Features

### Client Credentials Flow
- ✅ **Machine-to-machine authentication** (no user interaction)
- ✅ **Automatic token renewal** before expiration
- ✅ **Secure token storage** in distributed cache
- ✅ **Configurable scopes** for least-privilege access

### WebApi Protection
- ✅ **JWT Bearer authentication** with HelseID validation
- ✅ **Scope-based authorization** using `[Scope]` attribute
- ✅ **Multiple authentication schemes** for different client types
- ✅ **Fallback policy** requiring authentication by default

## Development Setup

### Prerequisites
1. **HelseID Client Registration** with:
   - Grant type: `client_credentials`
   - Scope: `fhi:webapi/health-records.read`
   - Client authentication method

2. **Development Certificates**:
   ```bash
   dotnet dev-certs https --trust
   ```

### Running the Integration

1. **Start WebApi**:
   ```bash
   cd samples/Fhi.Samples.WebApi
   dotnet run
   # API available at https://localhost:7150
   ```

2. **Configure Client Credentials** in `appsettings.Development.json`:
   ```json
   {
     "RefitClientCredentials": {
       "ClientId": "your-actual-client-id",
       "ClientSecret": "your-actual-client-secret"
     }
   }
   ```

3. **Start Refit Client**:
   ```bash
   cd samples/Fhi.Samples.RefitClientCredentials
   dotnet run
   ```

### Expected Logs

**Successful Integration**:
```
info: Calling Health Records API...
info: Successfully retrieved 3 health records:
info: Health Record - Name: Sample Record 1, Description: Sample description
```

**Token Acquisition** (with debug logging):
```
dbug: Duende.AccessTokenManagement: Requesting client credentials token for client HealthRecordsApiClient
dbug: Duende.AccessTokenManagement: Token response successful
```

## Troubleshooting

| Issue | Possible Cause | Solution |
|-------|---------------|----------|
| `401 Unauthorized` | Invalid client credentials | Verify ClientId/ClientSecret in HelseID |
| `403 Forbidden` | Missing scope | Ensure client has `fhi:webapi/health-records.read` scope |
| `Connection refused` | WebApi not running | Start WebApi on `https://localhost:7150` |
| `SSL errors` | Untrusted dev certificate | Run `dotnet dev-certs https --trust` |

## Extension Points

This integration demonstrates how to:

1. **Add Custom Scopes**: Update both client and WebApi scope requirements
2. **Multiple APIs**: Register additional Refit clients for different services
3. **Custom Token Handling**: Implement `ITokenHandler` for custom token logic
4. **Retry Policies**: Add Polly for resilient API calls
5. **Logging**: Enhance logging for token acquisition and API calls

## Related Documentation

- [OAuth 2.0 Client Credentials Grant](https://tools.ietf.org/html/rfc6749#section-4.4)
- [HelseID Documentation](https://docs.helseid.no/)
- [Duende AccessTokenManagement](https://docs.duendesoftware.com/identityserver/v6/tokens/extension_grants/token_exchange/)
- [Refit Documentation](https://github.com/reactiveui/refit)
