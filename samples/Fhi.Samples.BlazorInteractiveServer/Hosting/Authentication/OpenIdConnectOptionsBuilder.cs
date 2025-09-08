using Microsoft.AspNetCore.Authentication.OpenIdConnect;

public static class OpenIdConnectOptionsExtension
{

    public static OpenIdConnectOptions WithAuthority(this OpenIdConnectOptions options, string? authority)
    {
        options.Authority = authority;
        return options;
    }

    public static OpenIdConnectOptions WithClientId(this OpenIdConnectOptions options, string? clientId)
    {
        options.ClientId = clientId;
        return options;
    }
    public static OpenIdConnectOptions WithClientSecret(this OpenIdConnectOptions options, string? clientSecret)
    {
        options.ClientSecret = clientSecret;
        return options;
    }

    public static OpenIdConnectOptions WithCallbackPath(this OpenIdConnectOptions options, string callbackPath)
    {
        options.CallbackPath = callbackPath;
        return options;
    }

    public static OpenIdConnectOptions WithResponseType(this OpenIdConnectOptions options, string responseType)
    {
        options.ResponseType = responseType;
        return options;
    }

    public static OpenIdConnectOptions WithScopes(this OpenIdConnectOptions options, IEnumerable<string> scopes)
    {
        options.Scope.Clear();
        foreach (var scope in scopes)
            options.Scope.Add(scope);
        return options;
    }

    public static OpenIdConnectOptions OnAuthorizationCodeReceived(this OpenIdConnectOptions options, Func<AuthorizationCodeReceivedContext, Task> handler)
    {
        options.Events ??= new OpenIdConnectEvents();
        options.Events.OnAuthorizationCodeReceived = handler;
        return options;
    }

    public static OpenIdConnectOptions OnTokenValidated(this OpenIdConnectOptions options, Func<TokenValidatedContext, Task> handler)
    {
        options.Events ??= new OpenIdConnectEvents();
        options.Events.OnTokenValidated = handler;
        return options;
    }

    public static OpenIdConnectOptions OnPushAuthorization(this OpenIdConnectOptions options, Func<PushedAuthorizationContext, Task> handler)
    {
        options.Events ??= new OpenIdConnectEvents();
        options.Events.OnPushAuthorization = handler;
        return options;
    }
}