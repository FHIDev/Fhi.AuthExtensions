using Fhi.Samples.EndUser.Angular.BFFApi.Services.Models;

namespace Fhi.Samples.EndUser.Angular.BFFApi.Services.Tokens
{
    public interface ITokenStore
    {
        Task SaveAsync(HttpContext context, TokenSet tokens);
        Task<TokenSet?> GetAsync(HttpContext context);
        Task ClearAsync(HttpContext context);
    }
}