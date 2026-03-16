using Fhi.Samples.EndUser.Angular.BFFApi.Services.DPoP;
using Fhi.Samples.EndUser.Angular.BFFApi.Services.Models;
using Fhi.Samples.EndUser.Angular.BFFApi.Services.Tokens;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Fhi.Samples.EndUser.Angular.BFFApi.Services.IDPorten
{
    public class IDPortenService : IIDPortenService
    {
        private readonly IConfiguration _cfg;
        private readonly IHttpClientFactory _http;
        private readonly IDPoPKeyStore _keyStore;
        private readonly IDPoPProofGenerator _proofGen;
        private readonly ITokenStore _tokenStore;

        public IDPortenService(
            IConfiguration cfg,
            IHttpClientFactory http,
            IDPoPKeyStore keyStore,
            IDPoPProofGenerator proofGen,
            ITokenStore tokenStore)
        {
            _cfg = cfg;
            _http = http;
            _keyStore = keyStore;
            _proofGen = proofGen;
            _tokenStore = tokenStore;
        }

        /// <summary>
        /// Generates a PKCE code verifier and code challenge.
        /// The verifier is stored in the user's session and later used when exchanging
        /// the authorization code for tokens. The challenge is sent to ID-porten.
        ///
        /// PKCE ensures that only this BFF instance can redeem the authorization code.
        /// </summary>
        private static (string Verifier, string Challenge) CreatePkce()
        {
            var verifier = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
                .TrimEnd('=').Replace('+', '-').Replace('/', '_');

            using var sha = SHA256.Create();
            var challengeBytes = sha.ComputeHash(Encoding.UTF8.GetBytes(verifier));
            var challenge = Convert.ToBase64String(challengeBytes)
                .TrimEnd('=').Replace('+', '-').Replace('/', '_');

            return (verifier, challenge);
        }

        /// <summary>
        /// Builds the authorization URL for ID-porten using PKCE.
        /// Stores the PKCE verifier in session and returns the URL that the controller
        /// will redirect the user to.
        ///
        /// This is the first step of the login flow.
        /// </summary>
        public Task<string> CreateAuthorizationUrlAsync(HttpContext context)
        {
            var (verifier, challenge) = CreatePkce();
            context.Session.SetString("pkce_verifier", verifier);

            var url = $"{_cfg["IDPorten:AuthorizationEndpoint"]}" +
                      $"?client_id={_cfg["IDPorten:ClientId"]}" +
                      $"&redirect_uri={Uri.EscapeDataString(_cfg["IDPorten:RedirectUri"]!)}" +
                      $"&response_type=code" +
                      $"&scope=openid%20profile" +
                      $"&code_challenge={challenge}" +
                      $"&code_challenge_method=S256";

            return Task.FromResult(url);
        }

        /// <summary>
        /// Exchanges the authorization code for tokens using PKCE + DPoP.
        /// - Reads the PKCE verifier from session
        /// - Generates a DPoP proof for the token endpoint
        /// - Sends a token request to ID-porten
        /// - Stores the resulting token set in the session
        ///
        /// This completes the OAuth2/OIDC token exchange.
        /// </summary>
        public async Task<TokenSet> ExchangeCodeForTokensAsync(HttpContext context, string code)
        {
            var verifier = context.Session.GetString("pkce_verifier")
                ?? throw new InvalidOperationException("Missing PKCE verifier");

            var client = _http.CreateClient("dpop"); // DPoP added with http handler

            var req = new HttpRequestMessage(HttpMethod.Post, _cfg["IDPorten:TokenEndpoint"])
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["grant_type"] = "authorization_code",
                    ["code"] = code,
                    ["redirect_uri"] = _cfg["IDPorten:RedirectUri"]!,
                    ["client_id"] = _cfg["IDPorten:ClientId"]!,
                    ["code_verifier"] = verifier
                })
            };

            var res = await client.SendAsync(req);
            var json = await res.Content.ReadAsStringAsync();

            var tokens = JsonSerializer.Deserialize<TokenSet>(json)
                ?? throw new Exception("Invalid token response");

            await _tokenStore.SaveAsync(context, tokens);

            return tokens;
        }

        /// <summary>
        /// Calls the UserInfo endpoint using the stored access token and a DPoP proof.
        /// This retrieves the authenticated user's profile information from ID-porten.
        ///
        /// The frontend never receives the access token directly — only the BFF calls UserInfo.
        /// </summary>
        public async Task<string> GetUserInfoAsync(HttpContext context)
        {
            var tokens = await _tokenStore.GetAsync(context)
                ?? throw new UnauthorizedAccessException();

            var client = _http.CreateClient("dpop"); // DPoP added with http handler

            var req = new HttpRequestMessage(HttpMethod.Get, _cfg["IDPorten:UserInfoEndpoint"]);
            req.Headers.Add("Authorization", $"DPoP {tokens.AccessToken}");

            var res = await client.SendAsync(req);
            var json = await res.Content.ReadAsStringAsync();

            return json;
        }
    }
}