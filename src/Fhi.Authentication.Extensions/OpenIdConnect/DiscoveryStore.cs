using Duende.IdentityModel.Client;
using System.Collections.Concurrent;

namespace Fhi.Authentication.OpenIdConnect
{
    /// <summary>
    /// A store for OpenID Connect discovery documents, allowing retrieval by authority.
    /// </summary>
    public interface IDiscoveryDocumentStore
    {
        IDiscoveryDocument Get(string authority);
    }

    internal class InMemoryDiscoveryDocumentStore : IDiscoveryDocumentStore
    {
        private readonly ConcurrentDictionary<string, IDiscoveryDocument> _discoveryCache = new();

        public InMemoryDiscoveryDocumentStore(IHttpClientFactory httpClientFactory, IEnumerable<string> authorities)
        {
            foreach (var authority in authorities)
            {
                var doc = LoadDiscoveryDocumentAsync(httpClientFactory, authority).GetAwaiter().GetResult();
                _discoveryCache.TryAdd(authority, new DiscoveryDocument(
                    authority,
                    doc.Issuer,
                    doc.AuthorizeEndpoint,
                    doc.TokenEndpoint,
                    doc.UserInfoEndpoint,
                    doc.JwksUri,
                    doc.EndSessionEndpoint));
            }
        }

        public IDiscoveryDocument Get(string authority)
        {
            if (!_discoveryCache.TryGetValue(authority, out var doc))
            {
                throw new KeyNotFoundException($"No discovery document found for authority '{authority}'.");
            }
            return doc;
        }

        private static async Task<DiscoveryDocumentResponse> LoadDiscoveryDocumentAsync(
            IHttpClientFactory httpClientFactory,
            string authority)
        {
            var client = httpClientFactory.CreateClient();
            var discoveryDoc = await client.GetDiscoveryDocumentAsync(authority);

            if (discoveryDoc.IsError)
            {
                throw new InvalidOperationException(
                    $"Error retrieving discovery document from {authority}: {discoveryDoc.Error}");
            }

            return discoveryDoc;
        }
    }
}

