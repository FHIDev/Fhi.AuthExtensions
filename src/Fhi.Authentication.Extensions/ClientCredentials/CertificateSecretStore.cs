using Fhi.Authentication.Tokens;
using Microsoft.Extensions.Logging;

namespace Fhi.Authentication.ClientCredentials;

/// <summary>
/// Secret store implementation that retrieves certificates from the Windows certificate store
/// and converts them to JWK format using the certificate resolver.
/// </summary>
internal class CertificateSecretStore : ISecretStore
{
    private readonly string _clientId;
    private readonly string _certificateThumbprint;
    private readonly ICertificateJwkResolver _certificateResolver;
    private readonly ILogger<CertificateSecretStore> _logger;

    public CertificateSecretStore(
        string clientId,
        string certificateThumbprint,
        ICertificateJwkResolver certificateResolver,
        ILogger<CertificateSecretStore> logger)
    {
        _clientId = clientId ?? throw new ArgumentNullException(nameof(clientId));
        _certificateThumbprint = certificateThumbprint ?? throw new ArgumentNullException(nameof(certificateThumbprint));
        _certificateResolver = certificateResolver ?? throw new ArgumentNullException(nameof(certificateResolver));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public string GetPrivateKeyAsJwk()
    {
        try
        {
            _logger.LogInformation("CertificateSecretStore: Retrieving certificate for client: {ClientId}", _clientId);

            // Use the certificate resolver to handle all certificate logic
            var certificateOptions = new CertificateOptions
            {
                Thumbprint = _certificateThumbprint,
                StoreLocation = CertificateStoreLocation.CurrentUser
            };

            var jwk = _certificateResolver.ResolveToJwk(certificateOptions);
            
            _logger.LogInformation("CertificateSecretStore: Successfully retrieved JWK for thumbprint: {Thumbprint}", _certificateThumbprint);
            
            return jwk;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CertificateSecretStore: Error retrieving certificate JWK for client: {ClientId}", _clientId);
            throw;
        }
    }
}