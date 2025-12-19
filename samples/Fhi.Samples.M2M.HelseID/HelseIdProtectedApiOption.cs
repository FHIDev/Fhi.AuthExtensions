using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace M2M.Host.HelseID;

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
    [Required] public required string PrivateJwk { get; init; }
}
