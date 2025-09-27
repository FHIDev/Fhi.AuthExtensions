using AngularBFF.Net8.Api.HealthRecords;
using Duende.AccessTokenManagement.DPoP;
using Duende.AccessTokenManagement.OpenIdConnect;
using Fhi.Authentication;
using Fhi.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddLogging();
builder.Services.AddHttpContextAccessor();
builder.Services.AddDistributedMemoryCache();

var authSettingsSection = builder.Configuration.GetSection("Authentication");
builder.Services.Configure<AuthenticationSettings>(authSettingsSection);
var authenticationSettings = authSettingsSection.Get<AuthenticationSettings>();

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
}).AddCookie(options =>
{
    /*****************************************************************************************
     * ExpireTimeSpan should be must be set out from access_token and refresh_token lifetime. This is to ensure 
     * that the cookie is not expired when the refresh token is expired used to get a new access token in downstream API calls. 
     * ***************************************************************************************/
    options.ExpireTimeSpan = TimeSpan.FromSeconds(90);
})
.AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
{
    options.Authority = authenticationSettings!.Authority;
    options.ClientId = authenticationSettings.ClientId;
    options.CallbackPath = "/signin-oidc";
    options.ResponseType = "code";

    /************************************************************************************
     * In a BFF (Backend for Frontend) application with a JavaScript (SPA) frontend, 
     * attempting to initiate an authentication redirect (302) in response to an XHR/fetch 
     * call will result in a CORS error, as browsers block automatic redirects to 
     * third-party identity providers in such cases.
     *
     * To handle this properly, the frontend listens for 401 Unauthorized responses and 
     * performs a full-page reload, navigating the browser to the /login endpoint.
     *
     * The /login endpoint then initiates a proper OpenID Connect authentication request 
     * via a 302 redirect to the Identity Provider (IdP), which is allowed by the browser 
     * because it was triggered by a full-page load (not an XHR).
     *
     * The code below ensures that when authentication is required for a non-/login route, 
     * a 401 is returned instead of a 302, allowing the frontend to detect the need for 
     * authentication and trigger the correct flow.
     *************************************************************************************/
    options.Events.OnRedirectToIdentityProvider = context =>
    {
        if (!context.Request.Path.StartsWithSegments("/login"))
        {
            context.Response.Headers.Location = context.ProtocolMessage.CreateAuthenticationRequestUrl();
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.HandleResponse();
        }
        return Task.CompletedTask;
    };

    /*********************************************************************************************
     * The events OnAuthorizationCodeReceived and OnPushAuthorization below is used to handle 
     * the authorization code flow with client assertion. This is required when using the client assertion
     * flow for authorization code exchange.
     *********************************************************************************************/
    options.Events.OnAuthorizationCodeReceived = context => context.AuthorizationCodeReceivedWithClientAssertionAsync(authenticationSettings.ClientSecret);
    options.Events.OnPushAuthorization = context => context.PushAuthorizationWithClientAssertion(authenticationSettings.ClientSecret);

    /*********************************************************************************************
     * The code below is claims and scope handling that will vary from application to application.
     *********************************************************************************************/
    options.Scope.Clear();
    options.Scope.Add("openid");
    options.Scope.Add("offline_access");
    options.Scope.Add("fhi:authextensions.samples/access");
});
builder.Services.AddOpenIdConnectCookieOptions();
builder.Services.AddSingleton<IPostConfigureOptions<OpenIdConnectOptions>, DefaultOpenIdConnectOptions>();

/**************************************************************************************
 * Registers support for managing access tokens used when calling downstream APIs 
 * on behalf of the authenticated user. This setup handles among other things storage, automatic renewal, 
 * and revocation of OpenID Connect tokens. Options set below is samples and is most likely not needed. It depends on the use cases.
 **************************************************************************************/
builder.Services.AddOpenIdConnectAccessTokenManagement(options =>
{
    options.RefreshBeforeExpiration = TimeSpan.FromSeconds(10);
    //options.ChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
    //Will create DPoP tokens for access token requests if DPoPJsonWebKey is set 
    options.DPoPJsonWebKey = DPoPProofKey.ParseOrDefault(authenticationSettings?.ClientSecret);
});

/**************************************************************************************
 * Registers a named HTTP client ("WebApi") that automatically includes the user's 
 * access token in outgoing requests to the downstream API. If the access token has 
 * expired, it will use the refresh token to obtain a new one before retrying the request.
 **************************************************************************************/
builder.Services.AddUserAccessTokenHttpClient(
    "WebApi",
    configureClient: (provider, client) =>
    {
        client.BaseAddress = new Uri("https://localhost:7150");
    });
builder.Services.AddTransient<IHealthRecordService, HealthRecordService>();

/************************************************************************************
The code below will require authentication on all incomming requests unless AllowAnonymous 
attribute is set. Note that minimal API endpoints are not always automatically protected by the policy.
*************************************************************************************/
builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build());

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

/************************************************************************************************
 * The code below is the login call from the frontend returning 302 redirect to the OIDC provider 
 ***********************************************************************************************/
app.MapGet("/login", [AllowAnonymous] async (HttpContext context) =>
{
    var returnUrl = context.Request.Query["returnUrl"].ToString();
    if (string.IsNullOrEmpty(returnUrl))
    {
        returnUrl = "/";
    }

    await context.ChallengeAsync(OpenIdConnectDefaults.AuthenticationScheme, new AuthenticationProperties { RedirectUri = returnUrl });
});

/************************************************************************************************
 * The code below is the session call from the frontend returning 200 OK with isAuthenticated = true/false.
 * It is used in the frontend bootstrap to check if the user is authenticated. If not authenticated, it 
 * will redirect to the /login endpoint.
 ***********************************************************************************************/
app.MapGet("/session", [AllowAnonymous] (HttpContext context) =>
{
    if (context.User?.Identity?.IsAuthenticated == true)
    {
        return Results.Ok(new { isAuthenticated = true });
    }
    return Results.Ok(new { isAuthenticated = false });
});

/************************************************************************************************
 * The code below is the logout call from the frontend. Must be logging out from both the cookie and 
 * OpenIdConnect authentication schemes. If other authentication schemes are used, it should logout 
 * from those as well. RevokeRefreshTokenAsync can also be handled in the CookieEvents class.
 ***********************************************************************************************/
app.MapGet("/logout", async (HttpContext context) =>
{
    await context.RevokeRefreshTokenAsync();

    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    await context.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme);
});

app.MapControllers();

/************************************************************************************************
* The code below is the fallback to the SPA. It will serve the index.html file for all requests that 
* are not handled by the API endpoints. This is typical in a SPA application where the frontend handles 
* routing and the backend serves the initial HTML file.
************************************************************************************************/
app.MapFallbackToFile("/index.html")
    .AllowAnonymous();

app.Run();

