using BlazorInteractiveServer.Hosting.Authentication;
using Client.BlazorInteractiveServer.Components;
using Client.BlazorInteractiveServer.Services;
using Duende.AccessTokenManagement;
using Duende.AccessTokenManagement.OpenIdConnect;
using Fhi.Authentication;
using Fhi.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;

internal static partial class Startup
{
    internal static WebApplicationBuilder ConfigureServices(this WebApplicationBuilder builder)
    {
        var authenticationSettingsSection = builder.Configuration.GetSection("Authentication");
        builder.Services.AddOptions<AuthenticationSettings>().Bind(authenticationSettingsSection).ValidateOnStart();
        var authenticationSettings = authenticationSettingsSection.Get<AuthenticationSettings>();

        builder.Services.AddAuthentication(options =>
        {
            options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
            options.DefaultSignOutScheme = OpenIdConnectDefaults.AuthenticationScheme;
        }).AddCookie(options =>
        {
            /*****************************************************************************************
            * ExpireTimeSpan should be set to a value before refresh token expires. This is to ensure that the cookie is not expired 
            * when the refresh token is expired used to get a new access token in downstream API calls. Default is 14 days. 
            * The AddOpenIdConnectCookieEventServices default is 60 minutes.
            * Note that the ExpireTimeSpan set below is sample. 
            * ***************************************************************************************/
            options.ExpireTimeSpan = TimeSpan.FromSeconds(6000);
        })
        .AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
        {
            options.Authority = authenticationSettings?.Authority;
            options.ClientId = authenticationSettings?.ClientId;
            options.CallbackPath = "/signin-oidc";
            options.ResponseType = "code";
            options.Events.OnAuthorizationCodeReceived = (context) =>
            {
                //var idportenKid = "abd7e1b6-40e3-4d42-846d-5d957eec4594";
                var ansattportenKid = "1705a2db-61cb-4e55-a493-7d0fe0cbb28e";
                var clientAssertion = GenerateClientAssertionFromPem(
                    issuer: authenticationSettings!.ClientId,
                    //audience: "https://test.idporten.no",
                    audience: "https://test.ansattporten.no",
                    privateKey: authenticationSettings.ClientSecret,
                    validForSeconds: 300, ansattportenKid);

                context.TokenEndpointRequest!.ClientAssertionType = "urn:ietf:params:oauth:client-assertion-type:jwt-bearer";
                context.TokenEndpointRequest.ClientAssertion = clientAssertion;
                return Task.CompletedTask;
            };
            options.PushedAuthorizationBehavior = PushedAuthorizationBehavior.Disable;
            //options.Events.OnAuthorizationCodeReceived = context => context.AuthorizationCodeReceivedWithClientAssertionAsync(authenticationSettings!.ClientSecret);
            //options.Events.OnPushAuthorization = context => context.PushAuthorizationWithClientAssertion(authenticationSettings!.ClientSecret);
            options.Events.OnTokenValidated = async context =>
            {
                if (context != null)
                {
                    await context.HttpContext.RequestServices
                    .GetRequiredService<IUserTokenStore>()
                    .StoreTokenAsync(context.Principal!, new UserToken
                    {
                        ClientId = ClientId.Parse(authenticationSettings?.ClientId ?? string.Empty),
                        AccessToken = AccessToken.Parse(context.TokenEndpointResponse?.AccessToken ?? string.Empty),
                        AccessTokenType = AccessTokenType.ParseOrDefault(context.TokenEndpointResponse?.TokenType),
                        Expiration = context.TokenEndpointResponse != null ? DateTimeOffset.UtcNow.AddSeconds(double.Parse(context.TokenEndpointResponse.ExpiresIn)) : default,
                        RefreshToken = RefreshToken.Parse(context.TokenEndpointResponse?.RefreshToken ?? string.Empty),
                        Scope = Scope.ParseOrDefault(context.TokenEndpointResponse?.Scope)
                    });
                }
            };
            options.MapInboundClaims = false;
            options.Scope.Clear();
            if (!string.IsNullOrWhiteSpace(authenticationSettings?.Scopes))
            {
                foreach (var scope in authenticationSettings.Scopes.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                {
                    options.Scope.Add(scope);
                }
            }
        });

        /*****************************************************************************************************************************
         * Add default handling for OpenIdConnect events using cookie authentication. This is used to handle token expiration for
         * downstream API calls and set default cookie options.
         **********************************************************************************************************************************/
        builder.Services.AddOpenIdConnectCookieOptions();
        builder.Services.AddSingleton<IPostConfigureOptions<OpenIdConnectOptions>, DefaultOpenIdConnectOptions>();


        /**************************************************************************************************************************************************
         * Handling downstream API call with client assertions.                                                                   *
         **************************************************************************************************************************************************/
        //builder.Services.AddTransient<IClientAssertionService, ClientAssertionService>();
        //builder.Services.AddSingleton<IDiscoveryCache>(serviceProvider =>
        //{
        //    var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
        //    return new DiscoveryCache(authenticationSettings!.Authority, () => httpClientFactory.CreateClient());
        //});

        /**************************************************************************************************************************************************
        * Handling downstream API call with token handling. Since Blazor uses SignalR, tokens are not available through httpcontext. Tokens must be stored *
        * in in another persistent secure storage available for the downstream API call                                                                   *
        /**************************************************************************************************************************************************/
        builder.Services.AddDistributedMemoryCache();
        builder.Services.AddOpenIdConnectAccessTokenManagement(options =>
        {
            //options.DPoPJsonWebKey = DPoPProofKey.ParseOrDefault(authenticationSettings?.ClientSecret);
        })
        .AddBlazorServerAccessTokenManagement<InMemoryUserTokenStore>();

        builder.Services.AddScoped<HealthRecordService>();
        builder.Services.AddUserAccessTokenHttpClient(
            "WebApi",
            configureClient: client =>
            {
                client.BaseAddress = new Uri("https://localhost:7150");
            });

        //TODO: Should create a Blazor project and nuget package
        builder.Services.AddScoped<AuthenticationStateProvider, CustomRevalidatingAuthenticationStateProvider>();
        builder.Services.AddCascadingAuthenticationState();
        builder.Services.AddScoped<NavigationService>();

        builder.Services.AddHttpContextAccessor();
        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents()
            .AddHubOptions(options =>
            {
                options.ClientTimeoutInterval = TimeSpan.FromSeconds(5);
                options.HandshakeTimeout = TimeSpan.FromSeconds(5);
            });

        builder.Services.AddAntiforgery();

        builder.Services.AddAuthorization(options =>
        {
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                        .RequireAuthenticatedUser()
                        .Build();
        });

        return builder;
    }

    internal static WebApplication ConfigurePipeline(this WebApplication app)
    {
        app.MapGet("/logout", async (HttpContext context) =>
        {
            await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            await context.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme);
        });

        app.UseStaticFiles();

        app.UseRouting();

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseAntiforgery();
        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();

        return app;
    }

    public static string GenerateClientAssertionFromPem(string issuer, string audience, string privateKey, int validForSeconds, string kid)
    {
        var rsa = RSA.Create();
        rsa.ImportFromPem(privateKey);

        var securityKey = new RsaSecurityKey(rsa)
        {
            KeyId = kid
        };
        var signingCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.RsaSha256);

        var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, issuer),
                //new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            };
        var payload = new JwtPayload(issuer, audience, claims, DateTime.UtcNow, DateTime.UtcNow.AddSeconds(validForSeconds));

        var header = new JwtHeader(signingCredentials, null, "client-authentication+jwt");

        var jwtSecurityToken = new JwtSecurityToken(header, payload);

        return new JwtSecurityTokenHandler().WriteToken(jwtSecurityToken);
    }


}
