using Microsoft.IdentityModel.Tokens;

namespace Fhi.Samples.EndUser.Angular.BFFApi.Services.Models
{
    public record DpopKeyPair(SecurityKey PrivateKey, JsonWebKey PublicJwk);
}