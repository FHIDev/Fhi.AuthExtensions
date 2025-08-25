//internal record ClientAuthentication(string Authority, string ClientId, string Scopes, string Secret, string SecretType);
//internal record HttpClientConfiguration(string Name, ClientAuthentication ClientAuthentication);

using System.ComponentModel.DataAnnotations;

public class HttpClientConfiguration
{
    [Required] public required string Name { get; set; }
    public required ClientAuthentication ClientAuthentication { get; set; } //= new ClientAuthentication(string.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
}

public class ClientAuthentication
{
    [Required] public string Authority { get; set; } = string.Empty;
    [Required] public string ClientId { get; set; } = string.Empty;
    public string Scopes { get; set; } = string.Empty;
    [Required] public string Secret { get; set; } = string.Empty;
    public string SecretType { get; set; } = string.Empty;
}
