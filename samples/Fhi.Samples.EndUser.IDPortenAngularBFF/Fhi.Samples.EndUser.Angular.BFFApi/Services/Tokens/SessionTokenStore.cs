using System.Text.Json;
using Fhi.Samples.EndUser.Angular.BFFApi.Services.Models;

namespace Fhi.Samples.EndUser.Angular.BFFApi.Services.Tokens
{
    public class SessionTokenStore : ITokenStore
    {
        private const string SessionKey = "OIDC_TOKEN_SET";

        public Task SaveAsync(HttpContext context, TokenSet tokens)
        {
            var json = JsonSerializer.Serialize(tokens);
            context.Session.SetString(SessionKey, json);
            return Task.CompletedTask;
        }

        public Task<TokenSet?> GetAsync(HttpContext context)
        {
            var json = context.Session.GetString(SessionKey);
            if (json == null)
                return Task.FromResult<TokenSet?>(null);

            var tokens = JsonSerializer.Deserialize<TokenSet>(json);
            return Task.FromResult(tokens);
        }

        public Task ClearAsync(HttpContext context)
        {
            context.Session.Remove(SessionKey);
            return Task.CompletedTask;
        }
    }
}