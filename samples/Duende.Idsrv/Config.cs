using Duende.IdentityServer;
using Duende.IdentityServer.Models;

namespace Workshop;

public static class Config
{
    public static IEnumerable<IdentityResource> IdentityResources =>
        new IdentityResource[]
        {
            new IdentityResources.OpenId(),
            new IdentityResources.Profile(),
        };

    public static IEnumerable<ApiScope> ApiScopes =>
        new ApiScope[]
        {
            new ApiScope("scope1"),
            new ApiScope("scope2"),
            new ApiScope("fhi:webapi/health-records.read"),
            new ApiScope("fhi:webapi/integration-access"),
            new ApiScope("fhi:webapi/access"),
            new ApiScope("fhi:lmr.internstatistikk/all")
        };

    public static IEnumerable<ApiResource> ApiResources =>
    new List<ApiResource>
    {
        new ApiResource("fhi:webapi", "Fhi Web api")
        {
            Scopes = { "fhi:webapi/health-records.read", "fhi:webapi/access"}
        },
         new ApiResource("fhi:lmr.internstatistikk", "fhi:lmr.internstatistikk")
        {
            Scopes = { "fhi:lmr.internstatistikk/all"}
        }
    };

    public static IEnumerable<Client> Clients =>
        new Client[]
        {
            // m2m client credentials flow client 
            new Client
            {
                ClientId = "m2m.client",
                ClientName = "Client Credentials Client",

                AllowedGrantTypes = GrantTypes.ClientCredentials,
                ClientSecrets =
                [
                    //new Secret("511536EF-F270-4058-80CA-1C89C192F69A".Sha256()),
                    //new Secret()
                    //{
                    //    Type = IdentityServerConstants.SecretTypes.JsonWebKey,
                    //    Value = "{'e': 'AQAB', 'key_ops': [],'kid': '5zEQg4m9HLKvi4G7Mxt0uONEzrTH5PSevpWbcr5v6WM','kty': 'RSA','n': 'yuSZlvn2-3OmMjdnPgmXtkYusxdqUzXrWxQ124VJxlPV0z6eqHpstmke8Wpo9p0xmSZ7KOsFImm5hspTN13dRBKmdeI_8fhsQWnl173LoyRt3wieuTIrKaVUz80zr_GakEBLgS5F6_PSqgnlLZ1qHez2bsjxHq90xr1anY_E9M9vgYahyvttjritS3l6FKqn6sznWp2BGTBAS3ZkKytJJJCIXJlj-2U4npzbEV6lINbT5nrPAyakLoRnMj9HpitP9IOmF886_JptrUMt9s5_7CnceorvcuMFqBdOKwBbmesTsqdPIyDPEN0HIHNLeZH3xZtRKfcoU_6qcZKAYW9mTQ','oth': [],'x5c': [] }"
                    //},
                    new Secret
                    {
                        Type = IdentityServerConstants.SecretTypes.JsonWebKey,
                        Value = @"{
                            ""e"": ""AQAB"",
                            ""kid"": ""5zEQg4m9HLKvi4G7Mxt0uONEzrTH5PSevpWbcr5v6WM"",
                            ""kty"": ""RSA"",
                            ""n"": ""yuSZlvn2-3OmMjdnPgmXtkYusxdqUzXrWxQ124VJxlPV0z6eqHpstmke8Wpo9p0xmSZ7KOsFImm5hspTN13dRBKmdeI_8fhsQWnl173LoyRt3wieuTIrKaVUz80zr_GakEBLgS5F6_PSqgnlLZ1qHez2bsjxHq90xr1anY_E9M9vgYahyvttjritS3l6FKqn6sznWp2BGTBAS3ZkKytJJJCIXJlj-2U4npzbEV6lINbT5nrPAyakLoRnMj9HpitP9IOmF886_JptrUMt9s5_7CnceorvcuMFqBdOKwBbmesTsqdPIyDPEN0HIHNLeZH3xZtRKfcoU_6qcZKAYW9mTQ""
                        }"
                    }
                ],
                AllowedScopes = { "fhi:webapi/health-records.read" }
            },

            // interactive client using code flow + pkce and shared secret
            new Client
            {
                ClientId = "interactive",
                ClientSecrets =
                {
                    new Secret("49C1A7E1-0C79-4A89-A3D6-A37998FB86B0".Sha256()),
                    new Secret
                    {
                        Type = IdentityServerConstants.SecretTypes.JsonWebKey,
                        Value = @"{
                            ""e"": ""AQAB"",
                            ""kid"": ""5zEQg4m9HLKvi4G7Mxt0uONEzrTH5PSevpWbcr5v6WM"",
                            ""kty"": ""RSA"",
                            ""n"": ""yuSZlvn2-3OmMjdnPgmXtkYusxdqUzXrWxQ124VJxlPV0z6eqHpstmke8Wpo9p0xmSZ7KOsFImm5hspTN13dRBKmdeI_8fhsQWnl173LoyRt3wieuTIrKaVUz80zr_GakEBLgS5F6_PSqgnlLZ1qHez2bsjxHq90xr1anY_E9M9vgYahyvttjritS3l6FKqn6sznWp2BGTBAS3ZkKytJJJCIXJlj-2U4npzbEV6lINbT5nrPAyakLoRnMj9HpitP9IOmF886_JptrUMt9s5_7CnceorvcuMFqBdOKwBbmesTsqdPIyDPEN0HIHNLeZH3xZtRKfcoU_6qcZKAYW9mTQ""
                        }"
                    }
                },


                AllowedGrantTypes = GrantTypes.Code,

                RedirectUris = { "https://localhost:7105/signin-oidc", "https://localhost:7151/signin-oidc", "https://localhost:5002/signin-oidc", "https://localhost:7122/signin-oidc", "https://localhost:4200/signin-oidc", "https://localhost:43371/signin-oidc" },
                FrontChannelLogoutUri = "https://localhost:7122/signout-oidc",
                PostLogoutRedirectUris = { "https://localhost:7122/signout-callback-oidc" },

                AllowOfflineAccess = true,
                RefreshTokenExpiration = TokenExpiration.Sliding,
                AbsoluteRefreshTokenLifetime = 5000,
                AccessTokenLifetime = 20,
                AllowedScopes = { "openid", "profile", "fhi:webapi/access", "fhi:lmr.internstatistikk/all" },
                AllowedCorsOrigins = { "https://localhost:7122" }
            },
            // interactive client using code flow + pkce and jwt secret
            new Client
            {
                ClientId = "client.jwt",

                ClientSecrets =
                {
                    //new Secret
                    //{
                    //    // base64 encoded X.509 certificate
                    //    Type = IdentityServerConstants.SecretTypes.X509CertificateBase64,

                    //    Value = "MIID...xBXQ="
                    //}
                    new Secret
                    {
                        // JWK formatted RSA key
                        Type = IdentityServerConstants.SecretTypes.JsonWebKey,

                        Value = "{'e':'AQAB','kid':'Zz...GEA','kty':'RSA','n':'wWw...etgKw'}"
                    }
                },

                AllowedGrantTypes = GrantTypes.Code,

                RedirectUris = { "https://localhost:7151/signin-oidc", "https://localhost:5002/signin-oidc", "https://localhost:7122/signin-oidc", "https://localhost:4200/signin-oidc", "https://localhost:43371/signin-oidc" },
                FrontChannelLogoutUri = "https://localhost:7122/signout-oidc",
                PostLogoutRedirectUris = { "https://localhost:7122/signout-callback-oidc" },

                AllowOfflineAccess = true,
                RefreshTokenExpiration = TokenExpiration.Sliding,
                AbsoluteRefreshTokenLifetime = 40,
                AccessTokenLifetime = 20,
                AllowedScopes = { "openid", "profile",  "fhi:webapi/access" },
                AllowedCorsOrigins = { "https://localhost:7122" }
            }
        };
}
