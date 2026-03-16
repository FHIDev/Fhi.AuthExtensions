using Fhi.Samples.EndUser.Angular.BFFApi.Services.IDPorten;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly IIDPortenService _idp;

    public AuthController(IIDPortenService idp)
    {
        _idp = idp;
    }

    /// <summary>
    /// Starts the login flow against ID-porten.
    /// Generates a PKCE code verifier + challenge, stores the verifier in session,
    /// builds the authorization URL, and redirects the user to ID-porten.
    /// </summary>
    [HttpGet("login")]
    public async Task<IActionResult> Login()
    {
        var url = await _idp.CreateAuthorizationUrlAsync(HttpContext);
        return Redirect(url);
    }

    /// <summary>
    /// Callback endpoint that ID-porten redirects the user back to after login.
    /// Receives the authorization code, exchanges it for tokens using PKCE + DPoP,
    /// stores the tokens in the user's session, and redirects back to the frontend.
    /// </summary>
    [HttpGet("callback")]
    public async Task<IActionResult> Callback(string code)
    {
        await _idp.ExchangeCodeForTokensAsync(HttpContext, code);
        return Redirect("/");
    }
}