using System.ComponentModel.DataAnnotations;

namespace Fhi.Samples.WorkerServiceMultipleClients.Configurations
{
    public class ApiClientSample1
    {
        [Required]
        public string? BaseAddress { get; set; }

        public string ClientName => nameof(ApiClientSample1);

        [Required]
        public required ClientCredentialsConfiguration ClientAuthentication { get; set; }
    }
}


//public class ClientAuthentication
//{
//    [Required] public string Authority { get; set; } = string.Empty;
//    [Required] public string ClientId { get; set; } = string.Empty;
//    public string Scopes { get; set; } = string.Empty;
//    [Required] public string Secret { get; set; } = string.Empty;
//    public string SecretType { get; set; } = string.Empty;
//}
