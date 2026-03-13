using Fhi.Samples.EndUser.Angular.BFFApi.Services.Models;

namespace Fhi.Samples.EndUser.Angular.BFFApi.Services.DPoP
{
    public interface IDPoPProofGenerator
    {
        Task<string> CreateProofAsync(string method, string url, DpopKeyPair keyPair);
    }
}