using System.Security.Cryptography.X509Certificates;

namespace Fhi.Authentication.Certificate;

/// <summary>
/// Configuration options for certificate-based authentication.
/// </summary>
public class CertificateOptions
{
    /// <summary>
    /// The certificate thumbprint (used when loading from certificate store).
    /// Spaces are optional and will be normalized.
    /// Priority: If both Thumbprint and PemCertificate are provided, Thumbprint takes precedence.
    /// </summary>
    public required string Thumbprint { get; init; }

    /// <summary>
    /// The certificate store location (CurrentUser or LocalMachine).
    /// Defaults to CurrentUser.
    /// Only used when Thumbprint is specified.
    /// </summary>
    public StoreLocation StoreLocation { get; init; } = StoreLocation.CurrentUser;

    /// <summary>
    /// 
    /// </summary>
    public StoreName StoreName { get; init; } = StoreName.My;
}