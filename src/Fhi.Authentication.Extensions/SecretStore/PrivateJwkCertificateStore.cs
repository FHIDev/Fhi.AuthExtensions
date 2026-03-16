using Fhi.Authentication.Certificate;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography.X509Certificates;

namespace Fhi.Authentication.SecretStore;

/// <summary>
/// Secret store implementation that retrieves certificates from the Windows certificate store
/// and converts them to JWK format using the certificate resolver.
/// Uses <see cref="ICertificateProvider"/> to find and validate certificate.
/// </summary>
internal class PrivateJwkCertificateStore : IPrivateJwkStore
{
    private readonly CertificateOptions _certificateOptions;
    private readonly ILogger<PrivateJwkCertificateStore> _logger;
    private readonly ICertificateProvider _certificateProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="PrivateJwkCertificateStore"/> class.
    /// </summary>
    /// <param name="certificateOptions">The certificate options containing thumbprint and store location.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="certificateProvider"></param>
    /// <exception cref="ArgumentNullException">Thrown if any required parameter is null.</exception>
    internal PrivateJwkCertificateStore(
        CertificateOptions certificateOptions,
        ILogger<PrivateJwkCertificateStore> logger,
        ICertificateProvider certificateProvider)
    {
        _certificateOptions = certificateOptions;
        _logger = logger;
        _certificateProvider = certificateProvider;
    }

    /// <inheritdoc/>
    public PrivateJwk? GetPrivateJwk()
    {
        try
        {
            var certificate = _certificateProvider.GetCertificate(_certificateOptions.Thumbprint, _certificateOptions.StoreName, _certificateOptions.StoreLocation);
            if (certificate != null)
            {
                if (certificate.HasPrivateKey)
                {
                    var validationResult = _certificateProvider.Validate(certificate);
                    if (validationResult.Success)
                    {
                        var privateKey = certificate.GetRSAPrivateKey()?.ExportRSAPrivateKeyPem();
                        if (privateKey == null)
                            return null;
                        return PrivateJwk.ParseFromPem(privateKey);
                    }
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CertificateSecretStore: Failed to retrieve JWK");
            throw;
        }
    }
}