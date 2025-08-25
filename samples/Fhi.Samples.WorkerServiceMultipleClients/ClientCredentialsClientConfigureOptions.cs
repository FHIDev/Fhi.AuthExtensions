using Duende.AccessTokenManagement;
using Microsoft.Extensions.Options;

internal class ClientCredentialsClientConfigureOptions : IConfigureNamedOptions<ClientCredentialsClient>
{
    private readonly IDiscoveryCacheFactory _discoveryCacheFactory;
    private readonly IOptions<List<HttpClientConfiguration>> _configs;

    public ClientCredentialsClientConfigureOptions(
        IDiscoveryCacheFactory discoveryCacheFactory,
        IOptions<List<HttpClientConfiguration>> configs)
    {
        _discoveryCacheFactory = discoveryCacheFactory;
        _configs = configs;
    }

    public void Configure(string? name, ClientCredentialsClient options)
    {
        if (!string.IsNullOrEmpty(name))
        {
            var config = _configs.Value.FirstOrDefault(c => c.Name == name);
            if (config == null) return;

            var disco = _discoveryCacheFactory.GetCache(name).GetAsync().GetAwaiter().GetResult();
            if (disco.IsError) throw new Exception($"Discovery failed for {name}: {disco.Error}");

            options.TokenEndpoint = disco.TokenEndpoint;
            options.ClientId = config.ClientAuthentication.ClientId;
            options.ClientSecret = config.ClientAuthentication.Secret;
            options.Scope = config.ClientAuthentication.Scopes;
        }
    }

    public void Configure(ClientCredentialsClient options) => Configure("", options);
}
