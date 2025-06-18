# Token expiration handling with Angular BFF Client and Downstream APIs

## Problem

When the `access_token` and `refresh_token` expire, but the **authentication cookie is still valid**, the user will **appear authenticated**, yet calls to downstream APIs will start to fail with `401 Unauthorized`. See [React to back-end changes](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/cookie?view=aspnetcore-9.0#react-to-back-end-changes)


## Alternative solutions

To avoid this mismatch there are several possible solutions: 

- **Use CookieEvent to validate the refresh token:** To ensure the session remains valid from the API’s perspective, proactively validate the refresh_token.
- **Implement a global exception handler for downstream API errors:** Set up a global exception handler to capture exceptions from downstream APIs. Specifically, handle 401 responses by throwing a 401 exception, which can then be handled appropriately by the client.
- **Create custom middleware for token management:** Develop middleware that checks the token’s expiration before forwarding requests to downstream APIs. If the token has expired, refresh it automatically before proceeding.
 
### Use CookieEvent to validate the refresh token

Override the `ValidatePrincipal` method on the CookieEvent and check the status of both the `access_token` and `refresh_token`. If the `access_token` is expired, attempt to use the `refresh_token` to obtain a new one. If that fails, reject the principal.

> 🔒 This logic must be combined with proper cookie expiration settings using `ExpireTimeSpan` to manage session longevity consistently.

 See sample code in[ Angular](https://github.com/FHIDev/Fhi.AuthExtensions/tree/main/samples/Fhi.Samples.AngularBFF) and [Blazor](https://github.com/FHIDev/Fhi.AuthExtensions/tree/main/samples/Fhi.Samples.BlazorInteractiveServer)

Do the following steps:

#### Step 1: Change Cookie expiration time 

ExpireTimeSpan should be must be set out from access_token and refresh_token lifetime. This is to ensure that the cookie is not expired after the refresh token is expired. 

Sample code
```
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
}).AddCookie(options =>
{
    options.ExpireTimeSpan = TimeSpan.FromSeconds(90);
})
...
```

#### Step 2: Add AddOpenIdConnectCookieOptions to the pipeline
You can use [`Fhi.Authentication.Extensions` package](https://www.nuget.org/packages/Fhi.Authentication.Extensions/) to validate the token expiration.

The `AddOpenIdConnectCookieOptions` will add `ValidatePrincipal` event that checks if token is expired, see implementation and handle `SignOut` [OpenIdConnectCookieEventsForApi](https://github.com/FHIDev/Fhi.AuthExtensions/blob/main/src/Fhi.Authentication.Extensions/OpenIdConnect/OpenIdConnectCookieEventsForApi.cs#L11)

```
builder.Services.AddOpenIdConnectCookieOptions();
```

### Create custom middleware for token management

The sample below uses [Duende accesstoken management `GetUserAccessTokenAsync` extension](https://docs.duendesoftware.com/accesstokenmanagement/web-apps/)

```csharp
public class TokenExpirationMiddleware
{
    private readonly RequestDelegate _next;

    public TokenExpirationMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var user = context.User;
        if (user.Identity is not null && user.Identity.IsAuthenticated)
        {
            var userToken = await context.GetUserAccessTokenAsync();
            if (userToken.IsError)
            {
                //Handle token expiration in your application in your preffered way
                //await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            }
        }

        await _next(context);
    }
}

```
