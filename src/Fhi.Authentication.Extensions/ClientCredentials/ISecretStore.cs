using Microsoft.Extensions.DependencyInjection;

namespace Fhi.Authentication.ClientCredentials
{
    /// <summary>
    /// Provides a contract for retrieving private keys in JWK format.
    /// </summary>
    /// <remarks>
    /// Implementations include:
    /// <list type="bullet">
    /// <item><see cref="FileSecretStore"/> - retrieves JWK from configuration or environment variables</item>
    /// <item><see cref="CertificateSecretStore"/> - retrieves certificates from Windows certificate store and converts to JWK</item>
    /// </list>
    /// </remarks>
    public interface ISecretStore
    {
        /// <summary>
        /// Retrieves the private key as a <see cref="PrivateJwk"/>.
        /// </summary>
        /// <returns>The private key in JWK format.</returns>
        PrivateJwk GetPrivateJwk();
    }
}