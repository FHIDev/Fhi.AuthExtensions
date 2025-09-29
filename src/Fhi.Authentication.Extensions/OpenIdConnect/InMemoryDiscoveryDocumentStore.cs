using Duende.IdentityModel.Client;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Fhi.Authentication.OpenIdConnect
{
    /// <summary>
    /// Options for the DiscoveryDocument store.
    /// </summary>
    public class DiscoveryDocumentStoreOptions
    {
        /// <summary>
        /// Url of the authority to load the discovery document from.
        /// </summary>
        public string Authority { get; set; } = string.Empty;
        /// <summary>
        /// Duration to cache the discovery document. Default is 24 hours.
        /// </summary>
        public TimeSpan CacheDuration { get; set; } = TimeSpan.FromHours(24);
    }

    internal class InMemoryDiscoveryDocumentStore : IDiscoveryDocumentStore
    {
        private readonly IMemoryCache _cache;
        private readonly IOptionsMonitor<DiscoveryDocumentStoreOptions> _options;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<InMemoryDiscoveryDocumentStore> _logger;

        public InMemoryDiscoveryDocumentStore(
            IMemoryCache cache,
            IOptionsMonitor<DiscoveryDocumentStoreOptions> options,
            IHttpClientFactory httpClientFactory,
            ILogger<InMemoryDiscoveryDocumentStore> logger)
        {
            _cache = cache;
            _options = options;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public IDiscoveryDocument Get(string authority)
        {
            var discoveryOptions = _options.Get(authority);
            return _cache.GetOrCreate(authority, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = discoveryOptions.CacheDuration;
                var doc = LoadDiscoveryDocumentAsync(_httpClientFactory, authority).GetAwaiter().GetResult();
                if (!doc.IsError)
                {
                    return new DiscoveryDocument(
                   authority,
                   doc.Issuer,
                   doc.AuthorizeEndpoint,
                   doc.TokenEndpoint,
                   doc.UserInfoEndpoint,
                   doc.JwksUri,
                   doc.EndSessionEndpoint);
                }
                _logger.LogError("Could not load Discovery document for Authority{authority}. Error {error}", authority, doc.Error);
                return null;
            })!;
        }

        private static async Task<DiscoveryDocumentResponse> LoadDiscoveryDocumentAsync(
            IHttpClientFactory httpClientFactory,
            string authority)
        {
            var client = httpClientFactory.CreateClient();
            var discoveryDoc = await client.GetDiscoveryDocumentAsync(authority);

            return discoveryDoc;
        }
    }
}

