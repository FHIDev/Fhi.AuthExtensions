using BlazorInteractiveServer.Hosting.Authentication;
using Duende.AccessTokenManagement;
using Duende.AccessTokenManagement.OpenIdConnect;
using Duende.IdentityModel.Client;
using Fhi.Authentication;
using Fhi.Authentication.OpenIdConnect;
using Fhi.Samples.BlazorInteractiveServer.Components;
using Fhi.Samples.BlazorInteractiveServer.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Options;

internal static partial class Startup
{
    internal static WebApplicationBuilder ConfigureServices(this WebApplicationBuilder builder)
    {
        var authenticationSettingsSection = builder.Configuration.GetSection("Authentication");
        builder.Services.Configure<AuthenticationSettings>(authenticationSettingsSection);
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
            options
               .WithAuthority(authenticationSettings?.Authority)
               .WithClientId(authenticationSettings?.ClientId)
               .WithClientSecret(authenticationSettings?.ClientSecret)
               .WithCallbackPath("/signin-oidc")
               .WithResponseType("code")
               .WithScopes(authenticationSettings?.Scopes?.Split(' ', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>())
               .OnAuthorizationCodeReceived(context => context.AuthorizationCodeReceivedWithClientAssertionAsync())
               .OnTokenValidated(async context =>
               {
                   var exp = DateTimeOffset.UtcNow.AddSeconds(double.Parse(context.TokenEndpointResponse!.ExpiresIn));
                   await context.HttpContext.RequestServices
                       .GetRequiredService<IUserTokenStore>()
                       .StoreTokenAsync(context.Principal!, new UserToken
                       {
                           AccessToken = context.TokenEndpointResponse.AccessToken,
                           AccessTokenType = context.TokenEndpointResponse.TokenType,
                           Expiration = exp,
                           RefreshToken = context.TokenEndpointResponse.RefreshToken,
                           Scope = context.TokenEndpointResponse.Scope
                       });
               })
               .OnPushAuthorization(context => context.PushAuthorizationWithClientAssertion());
            //options.Authority = authenticationSettings?.Authority;
            //options.ClientId = authenticationSettings?.ClientId;
            //options.ClientSecret = authenticationSettings?.ClientSecret;
            //options.CallbackPath = "/signin-oidc";
            //options.ResponseType = "code";
            //options.Events.OnAuthorizationCodeReceived = context => context.AuthorizationCodeReceivedWithClientAssertionAsync();
            //options.Events.OnPushAuthorization = context => context.PushAuthorizationWithClientAssertion();
            //options.Events.OnTokenValidated = async context =>
            //{
            //    var exp = DateTimeOffset.UtcNow.AddSeconds(double.Parse(context.TokenEndpointResponse!.ExpiresIn));
            //    await context.HttpContext.RequestServices
            //        .GetRequiredService<IUserTokenStore>()
            //        .StoreTokenAsync(context.Principal!, new UserToken
            //        {
            //            AccessToken = context.TokenEndpointResponse.AccessToken,
            //            AccessTokenType = context.TokenEndpointResponse.TokenType,
            //            Expiration = exp,
            //            RefreshToken = context.TokenEndpointResponse.RefreshToken,
            //            Scope = context.TokenEndpointResponse.Scope
            //        });
            //};

            //options.Scope.Clear();
            //if (!string.IsNullOrWhiteSpace(authenticationSettings?.Scopes))
            //{
            //    foreach (var scope in authenticationSettings.Scopes.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            //    {
            //        options.Scope.Add(scope);
            //    }
            //}
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
        builder.Services.AddTransient<IClientAssertionService, ClientAssertionService>();
        builder.Services.AddSingleton<IDiscoveryCache>(serviceProvider =>
        {
            var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
            return new DiscoveryCache(authenticationSettings!.Authority, () => httpClientFactory.CreateClient());
        });

        /**************************************************************************************************************************************************
        * Handling downstream API call with token handling. Since Blazor uses SignalR, tokens are not available through httpcontext. Tokens must be stored *
        * in in another persistent secure storage available for the downstream API call                                                                   *
        /**************************************************************************************************************************************************/
        builder.Services.AddDistributedMemoryCache();
        builder.Services.AddOpenIdConnectAccessTokenManagement(options =>
        {
            options.DPoPJsonWebKey = authenticationSettings?.ClientSecret;
        })
        .AddBlazorServerAccessTokenManagement<InMemoryUserTokenStore>();

        builder.Services.AddScoped<HealthRecordService>();
        builder.Services.AddUserAccessTokenHttpClient(
            "WebApi",
            parameters: new UserTokenRequestParameters()
            {
                SignInScheme = OpenIdConnectDefaults.AuthenticationScheme,
                /******************************************************************************************
                 * Optionally clientAssertion can be set as parameter or it will by default use IClientAssertionService
                 *****************************************************************************************/
                //Assertion = new ClientAssertion
                //{
                //    Type = OidcConstants.ClientAssertionTypes.JwtBearer,
                //    Value = ClientAssertionTokenHandler.CreateJwtToken(authenticationSettings?.ClientId,
                //        "issuer", //Manually set issuer or use the discovery document
                //        authenticationSettings.ClientSecret)
                //}
            },
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


}
