using System.ComponentModel.DataAnnotations;

namespace Fhi.Samples.WorkerServiceMultipleClients.MultipleClientVariant1.Configurations
{
    public class OidcClientOption
    {
        [Required] public string Authority { get; set; } = string.Empty;

        [Required] public string ClientId { get; set; } = string.Empty;

        [Required] public string Secret { get; set; } = string.Empty;
        [Required] public string SecretType { get; set; } = string.Empty;

        public bool UseDpop { get; set; } = false;

        public string Scope { get; set; } = string.Empty;
    }
}
