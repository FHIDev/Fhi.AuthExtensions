using System.ComponentModel.DataAnnotations;
using Fhi.Authentication.ClientCredentials;
using Microsoft.Extensions.Options;

namespace M2M.Host.HelseID;

/// <summary>
/// Configuration for HelseID API with certificate-based authentication.
/// Alternative to HelseIdProtectedApiOption that uses certificate instead of inline JWK.
/// </summary>
public record HelseIdCertificateApiOption
{
    [Required] public required string BaseAddress { get; init; }

    [Required]
    [ValidateObjectMembers]
    public required HelseIDCertificateAuthentication Authentication { get; init; }

    public const string ClientName = "HelseIdProtectedApi";
}

public record HelseIDCertificateAuthentication
{
    [Required] public required string Authority { get; init; }
    [Required] public required string ClientId { get; init; }
    [Required] public required string Scope { get; init; }

    /// <summary>
    /// Certificate configuration - will be converted to JWK by IPrivateKeyHandler with format auto-detection
    /// </summary>
    [Required]
    [ValidateObjectMembers]
    public required CertificateOptions Certificate { get; init; }
}