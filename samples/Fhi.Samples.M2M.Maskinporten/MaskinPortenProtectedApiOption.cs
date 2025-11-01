using System.ComponentModel.DataAnnotations;

namespace M2M.Host.Maskinporten
{
    public class MaskinPortenProtectedApiOption
    {
        [Required] public string BaseAddress { get; set; } = string.Empty;
        [Required] public MaskinPortenClientAuthentication Authentication { get; set; } = new MaskinPortenClientAuthentication();
        public string ClientName => "MaskinPorteProtectedApi";
    }

    public class MaskinPortenClientAuthentication
    {
        [Required] public string Environment { get; set; } = string.Empty;
        [Required] public string ClientId { get; set; } = string.Empty;

        [Required] public string Scope { get; set; } = string.Empty;
        [Required] public string PrivateJwk { get; set; } = string.Empty;

        public string? Resource { get; set; }

    }
}
