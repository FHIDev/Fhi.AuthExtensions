using Duende.IdentityModel.Client;
using Microsoft.Extensions.Caching.Memory;

public interface IOidcDiscoveryService
{
    Task<DiscoveryDocumentResponse?> GetDiscoveryDocument(string authority, TimeSpan? cacheExpiration = null);
}

public class DefaultOidcDiscoveryService : IOidcDiscoveryService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _cache;

    public DefaultOidcDiscoveryService(IHttpClientFactory httpClientFactory, IMemoryCache cache)
    {
        _httpClientFactory = httpClientFactory;
        _cache = cache;
    }

    private TimeSpan _defaultCacheExpiration => TimeSpan.FromHours(24);

    public async Task<DiscoveryDocumentResponse?> GetDiscoveryDocument(string authority, TimeSpan? cacheExpiration = null)
    {
        var cacheKey = $"oidc-discovery::{authority}";
        return await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = cacheExpiration ?? _defaultCacheExpiration;

            var client = _httpClientFactory.CreateClient();
            var response = await client.GetDiscoveryDocumentAsync(authority);

            if (response.IsError)
            {
                _cache.Remove(cacheKey);
                throw new InvalidOperationException(
                    $"Failed to get discovery document from {authority}: {response.Error}");
            }

            return response;
        });
    }
}