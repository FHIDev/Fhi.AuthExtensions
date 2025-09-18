using Duende.IdentityModel.Client;
using Microsoft.Extensions.Caching.Memory;

public interface IOidcDiscoveryService
{
    DiscoveryDocumentResponse? GetValue(string key);
    Task<DiscoveryDocumentResponse> GetDiscoveryDocument(string authority, TimeSpan? cacheExpiration = null);
}
public class DefaultOidcDiscoveryService : IOidcDiscoveryService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _cache;

    public DefaultOidcDiscoveryService(
        IHttpClientFactory httpClientFactory,
        IMemoryCache cache,
        IEnumerable<string> authorities)
    {
        _httpClientFactory = httpClientFactory;
        _cache = cache;
        foreach (var authority in authorities)
        {
            LoadDataIntoCache(authority);
        }
    }

    public async Task<DiscoveryDocumentResponse> GetDiscoveryDocument(string authority, TimeSpan? cacheExpiration = null)
    {
        var client = _httpClientFactory.CreateClient();
        return await client.GetDiscoveryDocumentAsync(authority);
    }

    private void LoadDataIntoCache(string authority)
    {
        var discoveryDoc = GetDiscoveryDocument(authority);
        _cache.Set(authority, discoveryDoc, new MemoryCacheEntryOptions
        {
            Priority = CacheItemPriority.NeverRemove
        });
    }

    public DiscoveryDocumentResponse? GetValue(string key)
    {
        if (_cache.TryGetValue(key, out DiscoveryDocumentResponse? data))
        {
            return data;
        }

        return default;
    }
}