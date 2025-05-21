# Fhi.Authentication.Extensions

Authentication extensions for ASP.NET Core.

## Features

- OpenID Connect authentication extensions
- Refit client with OAuth 2.0 client credentials support
- Automatic access token management and refresh
- Built-in token caching for performance
- Comprehensive error handling and validation

## Usage

### OpenID Connect Extensions

```csharp
services.AddOpenIdConnectCookieOptions();
```

### Refit Client with Client Credentials

Configure a Refit client that automatically handles OAuth 2.0 client credentials authentication:

#### 1. Add to your `appsettings.json`:

```json
{
  "RefitClientCredentials": {
    "TokenEndpoint": "https://your-auth-server.com/connect/token",
    "ClientId": "your-client-id",
    "ClientSecret": "your-client-secret",
    "Scope": "api.read api.write",
    "ApiBaseUrl": "https://your-api.com",
    "ClientName": "MyApiClient"
  }
}
```

#### 2. Register your Refit client:

```csharp
// Define your API interface
public interface IMyApi
{
    [Get("/data")]
    Task<MyData> GetDataAsync();
    
    [Post("/data")]
    Task<MyData> CreateDataAsync([Body] CreateDataRequest request);
}

// Register in Program.cs or Startup.cs
builder.Services.AddRefitClientWithClientCredentials<IMyApi>(builder.Configuration);
```

#### 3. Use the client in your services:

```csharp
public class MyService
{
    private readonly IMyApi _myApi;

    public MyService(IMyApi myApi)
    {
        _myApi = myApi;
    }

    public async Task<MyData> GetDataAsync()
    {
        // Token is automatically acquired and attached
        return await _myApi.GetDataAsync();
    }
}
```

#### Configuration Options

| Property | Required | Description |
|----------|----------|-------------|
| `TokenEndpoint` | ✅ | OAuth 2.0 token endpoint URL |
| `ClientId` | ✅ | OAuth 2.0 client identifier |
| `ClientSecret` | ❌ | OAuth 2.0 client secret (optional for some grant types) |
| `Scope` | ❌ | Requested scopes (space-separated) |
| `ApiBaseUrl` | ❌ | Base URL for the API (can be set via HttpClient configuration) |
| `ClientName` | ❌ | Name for the token client (defaults to "default") |

#### Advanced Configuration

You can customize Refit settings and HttpClient configuration:

```csharp
builder.Services.AddRefitClientWithClientCredentials<IMyApi>(
    builder.Configuration,
    refitSettings =>
    {
        // Configure Refit settings
        refitSettings.ContentSerializer = new SystemTextJsonContentSerializer();
    },
    httpClient =>
    {
        // Configure HttpClient
        httpClient.Timeout = TimeSpan.FromSeconds(30);
        httpClient.DefaultRequestHeaders.Add("User-Agent", "MyApp/1.0");
    });
```

## Examples

See the [RefitClientCredentialExample](../../samples/RefitClientCredentialExample/) sample project for a complete working example.

