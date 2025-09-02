using System.ComponentModel.DataAnnotations;

namespace Fhi.Samples.WorkerServiceMultipleClients.Configurations
{
    public class ApiClientSample2
    {
        [Required]
        public string? BaseAddress { get; set; }

        public string ClientName => nameof(ApiClientSample2);

        [Required]
        public ClientCredentialsConfiguration? ClientAuthentication { get; set; }
    }
}

