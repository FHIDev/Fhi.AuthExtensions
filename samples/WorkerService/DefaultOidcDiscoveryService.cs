using Duende.IdentityModel.Client;

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

        //var cacheKey = $"oidc-discovery::{authority}";
        //return _cache.GetOrCreate(cacheKey, entry =>
        //{
        //    entry.AbsoluteExpirationRelativeToNow = cacheExpiration ?? _defaultCacheExpiration;

        //    var client = _httpClientFactory.CreateClient();
        //    var response = client.GetDiscoveryDocumentAsync(authority).GetAwaiter().GetResult();

        //    if (response.IsError)
        //    {
        //        _cache.Remove(cacheKey);
        //        throw new InvalidOperationException(
        //            $"Failed to get discovery document from {authority}: {response.Error}");
        //    }

        //    return response;
        //});
    }
}
