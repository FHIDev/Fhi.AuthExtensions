using Microsoft.Extensions.Caching.Distributed;

namespace Fhi.Authentication.JwtDPoP.Validation
{
    internal interface IReplayCache
    {
        Task Add(string jtiHash, DateTimeOffset expiration, CancellationToken cancellationToken = default);
        Task<bool> Exists(string jtiHash, CancellationToken cancellationToken = default);
    }

    internal class ReplayCache : IReplayCache
    {
        private const string Prefix = "DPoP-Replay-jti-";
        private readonly IDistributedCache _cache;

        public ReplayCache(IDistributedCache cache) => _cache = cache;

        public async Task Add(string handle, DateTimeOffset expiration, CancellationToken cancellationToken)
        {
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpiration = expiration
            };
            await _cache.SetAsync(Prefix + handle, Array.Empty<byte>(), options, cancellationToken);
        }

        public async Task<bool> Exists(string handle, CancellationToken cancellationToken)
            => await _cache.GetAsync(Prefix + handle, cancellationToken) != null;
    }

}
