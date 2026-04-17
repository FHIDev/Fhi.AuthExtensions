namespace Fhi.Samples.WebApi.IdPorten.Hosting;

public class AltinnSettings
{
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// The Altinn resource identifier to check authorization against, e.g. "urn:altinn:resource:example-resource".
    /// </summary>
    public string ResourceId { get; set; } = string.Empty;

    /// <summary>
    /// The action to check authorization for, e.g. "read", "write", "sign".
    /// </summary>
    public string Action { get; set; } = "read";
}
