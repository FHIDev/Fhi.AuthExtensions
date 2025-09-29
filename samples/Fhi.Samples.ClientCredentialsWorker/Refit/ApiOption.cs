using System.ComponentModel.DataAnnotations;

namespace Fhi.Samples.WorkerServiceMultipleClients.Refit
{
    internal class ApiOption
    {
        public string ClientName => "Api";

        [Required] public string BaseAddress { get; set; } = string.Empty;

        public ClientCredentialsConfiguration ClientAuthentication { get; set; } = new ClientCredentialsConfiguration();

    }

    internal class ClientCredentialsConfiguration
    {
        [Required] public string Authority { get; set; } = string.Empty;

        [Required] public string ClientId { get; set; } = string.Empty;

        [Required] public string Secret { get; set; } = string.Empty;

        public string Scope { get; set; } = string.Empty;
    }
}
