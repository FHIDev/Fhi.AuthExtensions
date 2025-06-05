# Require authentication by default in .NET Core
To require authentication by default on all incomming requests, configure a fallback authorization policy in your application's service pipeline. The policy will automatically require users to be authenticated for all endpoints unless explicitly overridden to allow anonymous access. This approach reduces the risk of accidentally exposing sensitive endpoints.


Code samples:

- [Angular BFF sample](https://github.com/FHIDev/Fhi.AuthExtensions/blob/main/samples/Fhi.Samples.AngularBFF/Fhi.Samples.Angular.BFFApi/Program.cs)
- [Web Api](https://github.com/FHIDev/Fhi.AuthExtensions/blob/main/samples/Fhi.Samples.WebApi/HostingExtensions.cs)


## 1: Add the fallback policy
All controllers and endpoints require an authenticated user. If a request is made to any endpoint and the user is not authenticated, a 401 Unauthorized response is returned. This is achieved using the `SetFallbackPolicy` method or `FallbackPolicy` option in the authorization configuration:

```
// Option 1: by default
builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build());

// Or option 2:
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();

// Or option 3: for multiple authentication schemes
builder.Services.AddAuthorizationBuilder()
                .SetFallbackPolicy(new AuthorizationPolicyBuilder()
                            .RequireAuthenticatedUser()
                            .Build())
                .AddPolicy("Scheme1", policy =>
                {
                    policy.AuthenticationSchemes.Add("Scheme1");
                    policy.RequireAuthenticatedUser();
                })
                 .AddPolicy("Scheme2", policy =>
                 {
                     policy.AuthenticationSchemes.Add("Scheme2");
                     policy.RequireAuthenticatedUser();
                 });
   
});
```


## 2: Permit anonymous access
To allow anonymous access to specific endpoints (such as /login, /session, or the SPA fallback) use `[AllowAnonymous]` or `AllowAnonymous()` attribute on specific endpoints to permit anonymous access.

```
// This endpoint allows anonymous access
app.MapGet("/login", [AllowAnonymous] async (HttpContext context) => { ... });

// For an SPA client like Angular should allow anonymous access to the index page
app.MapFallbackToFile("/index.html").AllowAnonymous();

```