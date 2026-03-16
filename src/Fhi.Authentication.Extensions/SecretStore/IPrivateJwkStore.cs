using Microsoft.Extensions.DependencyInjection;

namespace Fhi.Authentication.SecretStore
{
    /// <summary>
    /// Provides a contract for retrieving private keys in JWK format.
    /// </summary>
    /// <remarks>
    /// Implementations include:
    /// <list type="bullet">
    /// <item><see cref="PrivateJwkCertificateStore"/> - retrieves certificates from Windows certificate store and converts to JWK</item>
    /// </list>
    /// </remarks>
    public interface IPrivateJwkStore
    {
        /// <summary>
        /// Retrieves the private key as a <see cref="PrivateJwk"/>.
        /// </summary>
        /// <returns>The private key</returns>
        PrivateJwk? GetPrivateJwk();
    }
}