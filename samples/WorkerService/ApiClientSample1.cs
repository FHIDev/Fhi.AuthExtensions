using System.ComponentModel.DataAnnotations;

public class ClientCredentialsConfiguration
{
    [Required] public string Authority { get; set; } = string.Empty;
    [Required] public string ClientId { get; set; } = string.Empty;
    public string Scope { get; set; } = string.Empty;
    [Required] public string Secret { get; set; } = string.Empty;
}

public class ApiClientSample1
{
    [Required]
    public string? BaseAddress { get; set; }

    public string ClientName => nameof(ApiClientSample1);

    [Required]
    public required ClientCredentialsConfiguration ClientAuthentication { get; set; }
}
