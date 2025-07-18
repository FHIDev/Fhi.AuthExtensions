using System.ComponentModel.DataAnnotations;

namespace WorkerService
{
    public class ClientConfiguration
    {
        /// <summary>
        /// Used for discovery document and manual processes
        /// </summary>
        [Required] public string Authority { get; set; } = string.Empty;
        [Required] public string ClientName { get; set; } = string.Empty;
        [Required] public string ClientId { get; set; } = string.Empty;
        [Required] public string Secret { get; set; } = string.Empty;
        public string? Scope { get; set; }

        /// <summary>
        /// Must be set for duende HttpClient extensions
        /// </summary>
        [Required] public string TokenEndpoint { get; set; } = string.Empty;
    }
}
