using Fhi.Authentication.Tokens;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Fhi.Authentication.ClientCredentials;

/// <summary>
/// Secret store implementation that retrieves certificates from the Windows certificate store
/// and converts them to JWK format using the certificate resolver.
/// Uses <see cref="CertificateSecretManager"/> for certificate discovery and filtering.
/// </summary>
public class CertificateSecretStore : ISecretStore
{
    private readonly CertificateOptions _certificateOptions;
    private readonly IPrivateKeyHandler _keyHandler;
    private readonly ILogger<CertificateSecretStore> _logger;
    private readonly CertificateSecretManager? _certificateManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="CertificateSecretStore"/> class.
    /// </summary>
    /// <param name="certificateOptions">The certificate options containing thumbprint and store location.</param>
    /// <param name="keyHandler">The key handler for converting certificates/secrets to JWK with format auto-detection.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="certificateManager">Optional certificate manager for discovery and filtering. If null, uses default behavior.</param>
    /// <exception cref="ArgumentNullException">Thrown if any required parameter is null.</exception>
    public CertificateSecretStore(
        CertificateOptions certificateOptions,
        IPrivateKeyHandler keyHandler,
        ILogger<CertificateSecretStore> logger,
        CertificateSecretManager? certificateManager = null)
    {
        _certificateOptions = certificateOptions ?? throw new ArgumentNullException(nameof(certificateOptions));
        _keyHandler = keyHandler ?? throw new ArgumentNullException(nameof(keyHandler));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _certificateManager = certificateManager;
        
        if (string.IsNullOrWhiteSpace(_certificateOptions.Thumbprint) && string.IsNullOrWhiteSpace(_certificateOptions.PemCertificate))
            throw new ArgumentException("Either Thumbprint or PemCertificate must be provided in CertificateOptions.", nameof(certificateOptions));
    }

    /// <inheritdoc/>
    public PrivateJwk GetPrivateJwk()
    {
        try
        {
            // Determine the input (thumbprint or PEM certificate)
            var input = _certificateOptions.Thumbprint ?? _certificateOptions.PemCertificate 
                ?? throw new InvalidOperationException("No certificate identifier provided in CertificateOptions.");
            
            if (_certificateManager != null && !string.IsNullOrWhiteSpace(_certificateOptions.Thumbprint))
            {
                _logger.LogDebug("CertificateSecretStore: Validating certificate with thumbprint: {Thumbprint}", 
                    _certificateOptions.Thumbprint);

                var certificate = _certificateManager.FindCertificate(_certificateOptions);
                if (certificate == null)
                {
                    throw new InvalidOperationException(
                        $"Certificate with thumbprint {_certificateOptions.Thumbprint} not found or did not pass validation filters.");
                }
                
                _logger.LogDebug("CertificateSecretStore: Certificate with thumbprint: {Thumbprint} validated", 
                    _certificateManager.GetCertificateDisplayName(certificate));
            }
            else
            {
                _logger.LogDebug("CertificateSecretStore: Retrieving certificate/key: {InputType}", 
                    !string.IsNullOrWhiteSpace(_certificateOptions.Thumbprint) ? "Thumbprint" : "PEM");
            }

            var jwkString = _keyHandler.GetPrivateJwk(input);
            
            _logger.LogInformation("CertificateSecretStore: Successfully retrieved and converted to JWK");

            return PrivateJwk.ParseFromJson(jwkString);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CertificateSecretStore: Failed to retrieve JWK");
            throw;
        }
    }
}