using BlazorApp.IdPorten.Components;
using BlazorApp.IdPorten.Hosting.Authentication;
using BlazorApp.IdPorten.Services;
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

namespace BlazorApp.IdPorten.Hosting;

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

            /************************************************************************************
             * ID-Porten uses private_key_jwt client authentication.
             * ClientSecret holds the PEM-encoded RSA private key.
             * ClientAssertionAudience is the token endpoint URL (Authority).
             * ClientAssertionKeyId is the kid registered with ID-Porten for this client.
             ************************************************************************************/
            options.Events.OnAuthorizationCodeReceived = (context) =>
            {
                var clientAssertion = GenerateClientAssertionFromPem(
                    issuer: authenticationSettings!.ClientId,
                    audience: authenticationSettings.ClientAssertionAudience,
                    privateKey: authenticationSettings.ClientSecret,
                    validForSeconds: 300,
                    kid: authenticationSettings.ClientAssertionKeyId);

                context.TokenEndpointRequest!.ClientAssertionType = "urn:ietf:params:oauth:client-assertion-type:jwt-bearer";
                context.TokenEndpointRequest.ClientAssertion = clientAssertion;
                return Task.CompletedTask;
            };
            options.PushedAuthorizationBehavior = PushedAuthorizationBehavior.Disable;

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
                        Scope = Scope.ParseOrDefault(context.TokenEndpointResponse?.Scope),
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
        * Handling downstream API call with token handling. Since Blazor uses SignalR, tokens are not available through httpcontext. Tokens must be stored
        * in another persistent secure storage available for the downstream API call.
        /**************************************************************************************************************************************************/
        builder.Services.AddDistributedMemoryCache();
        builder.Services.AddOpenIdConnectAccessTokenManagement()
            .AddBlazorServerAccessTokenManagement<InMemoryUserTokenStore>();

        builder.Services.AddScoped<ApiRequestService>();
        builder.Services.AddUserAccessTokenHttpClient(
            "IdPortenApi",
            configureClient: client =>
            {
                client.BaseAddress = new Uri("https://localhost:7165");
            });

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

    /// <summary>
    /// Generates a JWT client assertion signed with the provided RSA PEM private key.
    /// Used for private_key_jwt client authentication towards ID-Porten.
    /// </summary>
    public static string GenerateClientAssertionFromPem(string issuer, string audience, string privateKey, int validForSeconds, string kid)
    {
        var rsa = RSA.Create();
        rsa.ImportFromPem(privateKey);

        var securityKey = new RsaSecurityKey(rsa) { KeyId = kid };
        var signingCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.RsaSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, issuer),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
        };
        var payload = new JwtPayload(issuer, audience, claims, DateTime.UtcNow, DateTime.UtcNow.AddSeconds(validForSeconds));
        var header = new JwtHeader(signingCredentials, null, "client-authentication+jwt");

        return new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(header, payload));
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
}
