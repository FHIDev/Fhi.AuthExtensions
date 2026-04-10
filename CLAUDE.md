# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this repo is

NuGet library packages for ASP.NET Core authentication and authorization, tailored for Norwegian public health infrastructure (HelseID and Maskinporten). Three publishable packages:

- **Fhi.Authentication.Extensions** — OpenID Connect flows, client credentials, token management
- **Fhi.Authorization.Extensions** — Authorization helpers
- **Fhi.Authentication.JwtDPoP** — DPoP (Demonstration of Proof-of-Possession) protection for API endpoints

## Build and test

```bash
dotnet build --configuration Release
dotnet test --configuration Release

# Run a single test project
dotnet test ./tests/Fhi.Authentication.Extensions.UnitTests/Fhi.Authentication.Extensions.UnitTests.csproj

# Run a single test by name
dotnet test --filter "FullyQualifiedName~MyTestClass.MyTestMethod"
```

CI builds against .NET 8, 9, and 10. Source libraries target `net9.0;net10.0` (JwtDPoP also includes `net8.0`).

## Solution structure

```
src/
  Fhi.Authentication.Extensions/     # Core OIDC + client credentials library
  Fhi.Authorization.Extensions/      # Authorization helpers
  Fhi.Authentication.JwtDPoP/        # DPoP JWT validation for APIs
tests/
  Fhi.Authentication.Extensions.UnitTests/
  Fhi.Authorization.Extensions.UnitTests/
  Fhi.Authentication.JwtDPoP.Tests/
  Fhi.Auth.IntegrationTests/
  Fhi.Auth.EndToEndTests/
samples/
  Clients/   # End-user auth samples (Blazor, Angular BFF) + M2M samples (HelseID, Maskinporten)
  Apis/      # API samples (plain JWT bearer and DPoP-protected)
```

## Key architectural patterns

**Fhi.Authentication.Extensions** is organized into three modules:
- `ClientCredentials/` — Machine-to-machine flows via Duende.AccessTokenManagement
- `OpenIdConnect/` — End-user OIDC flows
- `Tokens/` — Token handling utilities

**Fhi.Authentication.JwtDPoP** validates DPoP proof tokens in APIs and is structured around:
- `Common/` — Shared DPoP utilities
- `Configurations/` — `JwtDPoPOptions`, `DPoPProofTokenValidationParameters`
- `Validation/` — The actual DPoP proof validation logic

**Dependency model**: Libraries depend on Duende.AccessTokenManagement and Microsoft.AspNetCore.Authentication. They do not depend on each other.

## Build configuration

- `Directory.Build.props` — global settings: implicit usings, nullable enabled, warnings as errors
- `Directory.Packages.props` — central package version management; framework-specific version overrides for ASP.NET Core packages
- `/src/Directory.Build.props` — adds NuGet metadata and XML doc generation for publishable packages

## Testing conventions

- Test framework: NUnit 4.x with NSubstitute for mocking
- Integration tests use setup helpers under each project's `Setup/` directory
- Test projects that need config files copy `.json` settings files to the output directory via MSBuild

## Releasing packages

Releases are done via manual GitHub Actions workflow dispatch (one per package). The workflow takes a version string (`X.Y.Z` or `X.Y.Z-betaN`), packs the NuGet, publishes to nuget.org, creates a git tag, and creates a draft GitHub release. Do not manually run `dotnet pack` to release.
