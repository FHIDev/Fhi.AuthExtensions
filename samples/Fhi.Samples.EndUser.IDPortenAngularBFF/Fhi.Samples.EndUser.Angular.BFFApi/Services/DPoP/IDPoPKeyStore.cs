using Fhi.Samples.EndUser.Angular.BFFApi.Services.Models;

namespace Fhi.Samples.EndUser.Angular.BFFApi.Services.DPoP
{
    public interface IDPoPKeyStore
    {
        Task<DpopKeyPair> GetKeyPairAsync();
    }
}