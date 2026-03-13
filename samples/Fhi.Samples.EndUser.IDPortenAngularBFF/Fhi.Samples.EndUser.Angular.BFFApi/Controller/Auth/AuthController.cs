using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Text;

namespace Fhi.Samples.EndUser.Angular.BFFApi.Controller.Auth
{
    [ApiController]
    [Route("auth")]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _config;

        public AuthController(IConfiguration config)
        {
            _config = config;
        }

        [HttpGet("login")]
        public IActionResult Login()
        {
            // 1. Generate PKCE code verifier
            var codeVerifier = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');

            // 2. Generate code challenge
            using var sha = SHA256.Create();
            var challengeBytes = sha.ComputeHash(Encoding.UTF8.GetBytes(codeVerifier));
            var codeChallenge = Convert.ToBase64String(challengeBytes)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');

            // 3. Store verifier in session
            HttpContext.Session.SetString("pkce_verifier", codeVerifier);

            // 4. Build redirect URL
            var authUrl = $"{_config["IDPorten:AuthorizationEndpoint"]}" +
                $"?client_id={_config["IDPorten:ClientId"]}" +
                $"&redirect_uri={Uri.EscapeDataString(_config["IDPorten:RedirectUri"]!)}" +
                $"&response_type=code" +
                $"&scope=openid%20profile" +
                $"&code_challenge={codeChallenge}" +
                $"&code_challenge_method=S256";

            return Redirect(authUrl);
        }
    }
}