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

