using System.ComponentModel.DataAnnotations;
using Fhi.Authentication.Tokens;

namespace Fhi.Authentication.ClientCredentials;

/// <summary>
/// Configuration options for certificate-based authentication.
/// </summary>
[CustomValidation(typeof(CertificateOptions), nameof(ValidateThumbprintOrPem))]
public class CertificateOptions
{
    /// <summary>
    /// The certificate thumbprint (used when loading from certificate store).
    /// Spaces are optional and will be normalized.
    /// Priority: If both Thumbprint and PemCertificate are provided, Thumbprint takes precedence.
    /// </summary>
    public string? Thumbprint { get; init; }

    /// <summary>
    /// The certificate store location (CurrentUser or LocalMachine).
    /// Defaults to CurrentUser.
    /// Only used when Thumbprint is specified.
    /// </summary>
    public CertificateStoreLocation StoreLocation { get; init; } = CertificateStoreLocation.CurrentUser;

    /// <summary>
    /// PEM-encoded certificate and private key in a single string.
    /// Should contain both the certificate (-----BEGIN CERTIFICATE-----)
    /// and private key (-----BEGIN PRIVATE KEY----- or -----BEGIN RSA PRIVATE KEY-----).
    /// Used as fallback if Thumbprint is not provided.
    /// </summary>
    public string? PemCertificate { get; init; }

    /// <summary>
    /// Validates that either Thumbprint or PemCertificate is provided.
    /// </summary>
    /// <param name="options">The CertificateOptions instance to validate.</param>
    /// <param name="context">The validation context.</param>
    /// <returns>ValidationResult.Success if valid; otherwise, a ValidationResult with error message.</returns>
    public static ValidationResult? ValidateThumbprintOrPem(CertificateOptions options, ValidationContext context)
    {
        if (string.IsNullOrWhiteSpace(options.Thumbprint) &&
            string.IsNullOrWhiteSpace(options.PemCertificate))
        {
            return new ValidationResult(
                "Either Thumbprint or PemCertificate must be provided",
                [nameof(Thumbprint), nameof(PemCertificate)]);
        }
        return ValidationResult.Success;
    }
}