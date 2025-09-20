using System.ComponentModel.DataAnnotations;

namespace Fhi.Samples.WorkerServiceMultipleClients.MultipleClientVariant2
{
    public class HelseIdProtectedApiOption
    {
        [Required] public string BaseAddress { get; set; } = string.Empty;
        [Required] public HelseIDClientAuthentication Authentication { get; set; } = new HelseIDClientAuthentication();
        public string ClientName => "HelseIdProtectedApi";
    }

    public class HelseIDClientAuthentication
    {
        [Required] public string Authority { get; set; } = string.Empty;

        [Required] public string ClientId { get; set; } = string.Empty;

        [Required] public string Scope { get; set; } = string.Empty;
        [Required] public string PrivateJwk { get; set; } = string.Empty;
    }

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
