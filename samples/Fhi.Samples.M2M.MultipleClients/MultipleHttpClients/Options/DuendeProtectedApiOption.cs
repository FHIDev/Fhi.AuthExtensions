using System.ComponentModel.DataAnnotations;

namespace Client.ClientCredentialsWorkers.MultipleHttpClients.Options
{
    public class DuendeProtectedApiOption
    {
        public string ClientName => "DuendeProtectedApi";
        [Required] public string BaseAddress { get; set; } = string.Empty;
        [Required] public DuendeClientAuthentication Authentication { get; set; } = new DuendeClientAuthentication();
    }

    public class DuendeClientAuthentication
    {
        [Required] public string Authority { get; set; } = string.Empty;

        [Required] public string ClientId { get; set; } = string.Empty;

        [Required] public string Scope { get; set; } = string.Empty;
        [Required] public string SharedSecret { get; set; } = string.Empty;
    }
}
