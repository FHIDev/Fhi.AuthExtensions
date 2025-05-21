# Refit Client Credentials Example

This sample demonstrates how to use the `AddRefitClientWithClientCredentials` extension method to create a Refit client that automatically handles OAuth 2.0 client credentials authentication.

## Overview

This example shows:

- ✅ **Automatic token management** using Duende.AccessTokenManagement
- ✅ **Refit client** with OAuth 2.0 client credentials flow
- ✅ **Background service** that periodically calls a protected API
- ✅ **Integration** with the sample WebApi project's health records endpoint

## Architecture

```
┌─────────────────┐    ┌──────────────────┐    ┌─────────────────┐
│                 │    │                  │    │                 │
│ Refit Client    │───▶│  Token Manager   │───▶│   HelseID STS   │
│ (This Sample)   │    │ (Duende.ATM)     │    │  (OAuth Server) │
│                 │    │                  │    │                 │
└─────────────────┘    └──────────────────┘    └─────────────────┘
         │                                              │
         │                                              │
         │              ┌─────────────────┐             │
         │              │                 │             │
         └─────────────▶│   WebApi        │◀────────────┘
                        │ /health-records │ (validates token)
                        │                 │
                        └─────────────────┘
```

## Configuration

### 1. Update Client Credentials

Edit `appsettings.Development.json` and replace the placeholder values:

```json
{
  "RefitClientCredentials": {
    "ClientName": "HealthRecordsApiClient",
    "TokenEndpoint": "https://helseid-sts.test.nhn.no/connect/token",
    "ClientId": "your-actual-client-id",
    "ClientSecret": "your-actual-client-secret", 
    "Scope": "fhi:webapi/health-records.read",
    "ApiBaseUrl": "https://localhost:7297"
  }
}
```

### 2. Required HelseID Configuration

Your HelseID client registration must include:

- **Grant Type**: `client_credentials`
- **Scope**: `fhi:webapi/health-records.read`
- **Client Authentication**: `client_secret_post` or `private_key_jwt`

## Running the Example

### Prerequisites

1. **Start the WebApi project** first:
   ```bash
   cd samples/Fhi.Samples.WebApi
   dotnet run
   ```
   The API will be available at `https://localhost:7150`

2. **Valid HelseID client credentials** with the required scope

### Run the Client

```bash
cd samples/Fhi.Samples.RefitClientCredentials
dotnet run
```

### Expected Output

If everything is configured correctly, you should see:

```
info: Fhi.Samples.RefitClientCredentials.Workers.HealthRecordsWorker[0]
      Health Records Worker running at: 06/02/2025 10:30:00 +00:00
info: Fhi.Samples.RefitClientCredentials.Workers.HealthRecordsWorker[0]
      Calling Health Records API...
info: Fhi.Samples.RefitClientCredentials.Workers.HealthRecordsWorker[0]
      Successfully retrieved 3 health records:
info: Fhi.Samples.RefitClientCredentials.Workers.HealthRecordsWorker[0]
      Health Record - Name: Sample Record 1, Description: Sample description, Created: 06/01/2025 08:00:00
```

## Troubleshooting

### Common Issues

1. **401 Unauthorized**
   - Verify your `ClientId` and `ClientSecret` are correct
   - Check that your client has the `fhi:webapi/health-records.read` scope
   - Ensure the token endpoint URL is correct

2. **Connection refused**
   - Make sure the WebApi project is running on `https://localhost:7150`
   - Check Windows Firewall/antivirus isn't blocking the connection

3. **SSL Certificate errors**
   - Run `dotnet dev-certs https --trust` to trust the development certificate

### Debug Logging

Enable detailed logging in `appsettings.Development.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Duende.AccessTokenManagement": "Debug",
      "Fhi.Authentication": "Debug"
    }
  }
}
```

## Key Components

### IHealthRecordsApi Interface

The Refit interface that defines the API contract:

```csharp
public interface IHealthRecordsApi
{
    [Get("/api/v1/integration/health-records")]
    Task<IEnumerable<HealthRecordDto>> GetHealthRecordsAsync();
}
```

### HealthRecordsWorker

Background service that:
- Calls the API every 10 seconds (configurable)
- Handles authentication automatically via the configured token handler
- Logs results and errors with appropriate detail

### AddRefitClientWithClientCredentials Extension

This extension method from `Fhi.Authentication.Extensions`:
- Configures Duende.AccessTokenManagement for client credentials flow
- Registers the Refit client with automatic token attachment
- Validates configuration at startup
- Adds distributed caching for token storage

## Related Documentation

- [Refit Documentation](https://github.com/reactiveui/refit)
- [Duende AccessTokenManagement](https://docs.duendesoftware.com/identityserver/v6/tokens/extension_grants/token_exchange/)
- [HelseID Documentation](https://docs.helseid.no/)
