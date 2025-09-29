namespace Fhi.Authentication.OpenIdConnect
{
    /// <summary>
    /// A store for OpenID Connect discovery documents, allowing retrieval by authority.
    /// </summary>
    public interface IDiscoveryDocumentStore
    {
        ///<inheritdoc/>
        IDiscoveryDocument Get(string authority);
    }
}

