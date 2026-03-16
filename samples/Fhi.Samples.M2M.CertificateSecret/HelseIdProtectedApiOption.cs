using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;

namespace M2M.Host.CertificateSecret;

public record HelseIdProtectedApiOption
{
    [Required] public required string BaseAddress { get; init; }

    [Required]
    [ValidateObjectMembers]
    public required HelseIDClientAuthentication Authentication { get; init; }
    public const string ClientName = "HelseIdProtectedApi";
}

public record HelseIDClientAuthentication
{
    [Required] public required string Authority { get; init; }
    [Required] public required string ClientId { get; init; }
    [Required] public required string Scope { get; init; }

    [Required] public required string CertificateThumbprint { get; init; }
}
