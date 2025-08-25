using Duende.IdentityModel.Client;
using Microsoft.Extensions.Options;

public interface IDiscoveryCacheFactory
{
    DiscoveryCache GetCache(string clientName);
}

internal class DiscoveryCacheFactory : IDiscoveryCacheFactory
{
    private readonly IDictionary<string, DiscoveryCache> _caches;

    public DiscoveryCacheFactory(IHttpClientFactory httpClientFactory, IOptions<List<HttpClientConfiguration>> options)
    {
        //var retriever = new HttpClientDiscoveryDocumentRetriever(httpClientFactory.CreateClient());
        _caches = new Dictionary<string, DiscoveryCache>(StringComparer.OrdinalIgnoreCase);

        foreach (var config in options.Value)
        {
            if (!string.IsNullOrEmpty(config.ClientAuthentication?.Authority))
            {
                //_caches[config.Name] = new DiscoveryCache(config.ClientAuthentication.Authority, retriever);
                _caches[config.Name] = new DiscoveryCache(config.ClientAuthentication.Authority);
            }
        }
    }

    public DiscoveryCache GetCache(string clientName)
    {
        if (_caches.TryGetValue(clientName, out var cache))
        {
            return cache;
        }
        throw new InvalidOperationException($"No DiscoveryCache registered for client '{clientName}'.");
    }
}
