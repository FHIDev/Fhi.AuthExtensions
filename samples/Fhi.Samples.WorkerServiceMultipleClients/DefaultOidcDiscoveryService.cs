using Duende.IdentityModel.Client;

//public interface IOidcDiscoveryService
//{
//    Task<DiscoveryDocumentResponse> GetDiscoveryDocument(string authority, TimeSpan? cacheExpiration = null);
//}

//public class DefaultOidcDiscoveryService : IOidcDiscoveryService
//{
//    private readonly IHttpClientFactory _httpClientFactory;
//    private readonly IDistributedCache _cache;

//    public DefaultOidcDiscoveryService(IHttpClientFactory httpClientFactory, IDistributedCache cache)
//    {
//        _httpClientFactory = httpClientFactory;
//        _cache = cache;
//    }

//    private TimeSpan _defaultCacheExpiration => TimeSpan.FromHours(24);


//    public async Task<DiscoveryDocumentResponse> GetDiscoveryDocument(
//       string authority,
//       TimeSpan? cacheExpiration = null)
//    {
//        var cacheKey = $"oidc-discovery::{authority}";

//        var cached = await _cache.GetStringAsync(cacheKey);
//        if (!string.IsNullOrEmpty(cached))
//        {
//            return JsonSerializer.Deserialize<DiscoveryDocumentResponse>(cached)!;
//        }

//        var client = _httpClientFactory.CreateClient();
//        var response = await client.GetDiscoveryDocumentAsync(authority);

//        if (response.IsError)
//        {
//            await _cache.RemoveAsync(cacheKey);
//            throw new InvalidOperationException(
//                $"Failed to get discovery document from {authority}: {response.Error}");
//        }

//        // Store in distributed cache
//        var options = new DistributedCacheEntryOptions
//        {
//            AbsoluteExpirationRelativeToNow = cacheExpiration ?? _defaultCacheExpiration
//        };

//        var serialized = JsonSerializer.Serialize(response);
//        await _cache.SetStringAsync(cacheKey, serialized, options);

//        return response;
//    }
//}


public interface IOidcDiscoveryService
{
    Task<DiscoveryDocumentResponse> GetDiscoveryDocument(string authority, TimeSpan? cacheExpiration = null);
}
public class DefaultOidcDiscoveryService : IOidcDiscoveryService
{
    private readonly IHttpClientFactory _httpClientFactory;

    public DefaultOidcDiscoveryService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<DiscoveryDocumentResponse> GetDiscoveryDocument(string authority, TimeSpan? cacheExpiration = null)
    {
        var client = _httpClientFactory.CreateClient();
        return await client.GetDiscoveryDocumentAsync(authority);
    }
}