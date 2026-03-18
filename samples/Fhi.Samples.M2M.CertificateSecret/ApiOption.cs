using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography.X509Certificates;

namespace M2M.Host.CertificateSecret;

public class ApiOption
{
    [Required] public required string BaseAddress { get; init; }

    [Required]
    public required ApiAuthentication Authentication { get; init; }
    public const string ClientName = "Api";
}

public class ApiAuthentication
{
    [Required] public required string Authority { get; init; }
    [Required] public required string ClientId { get; init; }
    [Required] public required string Scope { get; init; }
    [Required] public required string CertificateThumbprint { get; init; }
    public StoreLocation CertificateStoreLocation { get; init; } = StoreLocation.CurrentUser;
}
