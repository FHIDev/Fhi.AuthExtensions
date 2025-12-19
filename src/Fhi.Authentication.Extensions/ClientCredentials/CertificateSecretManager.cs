using Fhi.Authentication.Tokens;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography.X509Certificates;

namespace Fhi.Authentication.ClientCredentials;

/// <summary>
/// Manages certificate discovery and filtering for secret stores, following the pattern of Azure's KeyVaultSecretManager.
/// Provides hooks for customizing which certificates are loaded and how they are processed.
/// </summary>
public class CertificateSecretManager
{
    /// <summary>
    /// Logger for certificate discovery and filtering operations.
    /// </summary>
    protected readonly ILogger<CertificateSecretManager> Logger;

    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="CertificateSecretManager"/> class.
    /// </summary>
    /// <param name="logger">The logger instance for certificate operations.</param>
    /// <param name="timeProvider">Optional time provider for testability. Defaults to <see cref="TimeProvider.System"/>.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="logger"/> is null.</exception>
    public CertificateSecretManager(
        ILogger<CertificateSecretManager> logger,
        TimeProvider? timeProvider = null)
    {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    private bool Load(X509Certificate2 certificate)
    {
        ArgumentNullException.ThrowIfNull(certificate);

        // Default: only load certificates with private keys that are not expired
        if (!certificate.HasPrivateKey)
        {
            Logger.LogDebug("Skipping certificate {Thumbprint} - no private key", certificate.Thumbprint);
            return false;
        }

        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;

        if (certificate.NotAfter.ToUniversalTime() < utcNow)
        {
            Logger.LogWarning("Skipping expired certificate {Thumbprint}, expired on {ExpiryDate:u}",
                certificate.Thumbprint, certificate.NotAfter.ToUniversalTime());
            return false;
        }

        if (certificate.NotBefore.ToUniversalTime() > utcNow)
        {
            Logger.LogWarning("Skipping certificate {Thumbprint}, not valid until {ValidFrom:u}",
                certificate.Thumbprint, certificate.NotBefore.ToUniversalTime());
            return false;
        }

        return true;
    }
    
    private string NormalizeThumbprint(string thumbprint)
    {
        if (string.IsNullOrWhiteSpace(thumbprint))
            throw new ArgumentException("Thumbprint cannot be null or empty.", nameof(thumbprint));
        
        return thumbprint.Replace(" ", "").ToUpperInvariant();
    }

    /// <summary>
    /// Finds a specific certificate by thumbprint from the certificate store.
    /// </summary>
    /// <param name="options">The certificate options containing the thumbprint and store location.</param>
    /// <returns>The certificate if found and passes the Load filter; otherwise, null.</returns>
    public virtual X509Certificate2? FindCertificate(CertificateOptions options)
    {
        if (options == null)
            throw new ArgumentNullException(nameof(options));

        if (string.IsNullOrWhiteSpace(options.Thumbprint))
            return null;

        var normalizedThumbprint = NormalizeThumbprint(options.Thumbprint);
        var storeLocation = options.StoreLocation == CertificateStoreLocation.CurrentUser 
            ? StoreLocation.CurrentUser 
            : StoreLocation.LocalMachine;

        Logger.LogInformation("Searching for certificate with thumbprint: {Thumbprint} in {StoreLocation}",
            normalizedThumbprint, storeLocation);

        using var store = new X509Store(StoreName.My, storeLocation);
        try
        {
            store.Open(OpenFlags.ReadOnly);
            
            var certificates = store.Certificates.Find(
                X509FindType.FindByThumbprint, 
                normalizedThumbprint, 
                validOnly: false);

            if (certificates.Count == 0)
            {
                Logger.LogWarning("Certificate with thumbprint {Thumbprint} not found in {StoreLocation}",
                    normalizedThumbprint, storeLocation);
                return null;
            }

            var certificate = certificates[0];
            
            if (!Load(certificate))
            {
                Logger.LogWarning("Certificate with thumbprint {Thumbprint} found but did not pass Load filter",
                    normalizedThumbprint);
                return null;
            }

            Logger.LogInformation("Successfully found certificate: Thumbprint={Thumbprint}, Subject={Subject}",
                certificate.Thumbprint, certificate.Subject);

            return certificate;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error finding certificate with thumbprint {Thumbprint}", normalizedThumbprint);
            throw;
        }
    }

    /// <summary>
    /// Gets the display name for a certificate (typically the subject name).
    /// Override this to customize how certificates are identified in logs.
    /// </summary>
    /// <param name="certificate">The certificate.</param>
    /// <returns>A display name for the certificate.</returns>
    public virtual string GetCertificateDisplayName(X509Certificate2 certificate)
    {
        if (certificate == null)
            throw new ArgumentNullException(nameof(certificate));

        return certificate.Subject;
    }
}

