using System.ComponentModel.DataAnnotations;

namespace Fhi.Samples.WorkerServiceMultipleClients.MultipleClientVariant1.Configurations
{
    public class ApiClientSample2
    {
        [Required]
        public string? BaseAddress { get; set; }

        public string ClientName => nameof(ApiClientSample2);

        [Required]
        public required string OidcClientName { get; set; }
    }
}

