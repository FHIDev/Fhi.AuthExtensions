using System.ComponentModel.DataAnnotations;

namespace Fhi.Samples.WorkerServiceMultipleClients.Configurations
{
    public class ClientCredentialsConfiguration
    {
        [Required] public string Authority { get; set; } = string.Empty;
        [Required] public string ClientId { get; set; } = string.Empty;
        public string Scope { get; set; } = string.Empty;
        [Required] public string Secret { get; set; } = string.Empty;
    }
}
