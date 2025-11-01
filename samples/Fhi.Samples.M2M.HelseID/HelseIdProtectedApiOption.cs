using System.ComponentModel.DataAnnotations;

namespace M2M.Host.HelseID
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
}
