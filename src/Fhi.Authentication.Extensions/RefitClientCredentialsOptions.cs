using System.ComponentModel.DataAnnotations;

namespace Fhi.Authentication.Extensions
{
    /// <summary>
    /// Configuration options for acquiring an OAuth 2.0 access token using client credentials grant for a Refit client.
    /// </summary>
    public class RefitClientCredentialsOptions
    {
        /// <summary>
        /// The configuration section name for these options.
        /// </summary>
        public const string SectionName = "RefitClientCredentials";

        /// <summary>
        /// The token endpoint URL of the authorization server.
        /// </summary>
        [Required]
        public string TokenEndpoint { get; set; } = string.Empty;

        /// <summary>
        /// The client ID.
        /// </summary>
        [Required]
        public string ClientId { get; set; } = string.Empty;

        /// <summary>
        /// The client secret.
        /// </summary>
        [Required]
        public string ClientSecret { get; set; } = string.Empty;

        /// <summary>
        /// The scope(s) to request for the access token.
        /// Can be a single scope or multiple scopes separated by spaces.
        /// </summary>
        public string? Scope { get; set; }

        /// <summary>
        /// Optional: The name of the token client configuration.
        /// Defaults to "default" if not specified.
        /// This name is used to register and retrieve the token client configuration
        /// with Duende.AccessTokenManagement.
        /// </summary>
        public string ClientName { get; set; } = "default";

        /// <summary>
        /// Optional: The base address of the API the Refit client will be calling.
        /// This can also be configured directly when setting up the HttpClient for the Refit client.
        /// </summary>
        [Url]
        public string? ApiBaseUrl { get; set; }
    }
}
