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

    [HttpGet("login")]
    public async Task<IActionResult> Login()
    {
        var url = await _idp.CreateAuthorizationUrlAsync(HttpContext);
        return Redirect(url);
    }

    [HttpGet("callback")]
    public async Task<IActionResult> Callback(string code)
    {
        await _idp.ExchangeCodeForTokensAsync(HttpContext, code);
        return Redirect("/");
    }
}