using Fhi.Samples.EndUser.Angular.BFFApi.Services.Models;

namespace Fhi.Samples.EndUser.Angular.BFFApi.Services.IDPorten
{
    public interface IIDPortenService
    {
        Task<string> CreateAuthorizationUrlAsync(HttpContext context);
        Task<TokenSet> ExchangeCodeForTokensAsync(HttpContext context, string code);
        Task<string> GetUserInfoAsync(HttpContext context);
    }
}
