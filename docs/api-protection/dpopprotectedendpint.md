# Protect API endpoints with DPoP

## Introduction

A DPoP bound token is an OAuth 2.0 token that allows detection of replay attacks. 

_The primary aim of DPoP is to prevent unauthorized or illegitimate parties from using leaked or stolen access tokens, by binding a token to a public key upon issuance and requiring that the client proves possession of the corresponding private key when using the token._[RFC 9449](https://www.rfc-editor.org/rfc/rfc9449.html) 

For more information see the [basic flow](https://www.rfc-editor.org/rfc/rfc9449.html#basic-flow) described in the [RFC 9449](https://www.rfc-editor.org/rfc/rfc9449.html) spesification.

## Code sample

To protect the API with DPoP token

```
builder
    .Services
    .AddAuthentication()
    .AddJwtDpop("DPOP", options =>
    {
        options.Audience = "fhi:api";
        options.Authority = "https://helseid-sts.test.nhn.no";
    });

```

Sample with overriding DPoP proof settings

```csharp

builder
    .Services
    .AddAuthentication()
    .AddJwtDpop(AuthenticationSchemes.HelseIdDPoP, options =>
        {
            options.Audience = "fhi:api";
            options.Authority = "https://helseid-sts.test.nhn.no";
            options.DPoPProofTokenValidationParameters = new DPoPProofTokenValidationParameters()
            {
                    ValidAlgorithms = new[] { "ES256", "RS256" }
            };
        });

```
