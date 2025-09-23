using Microsoft.Extensions.DependencyInjection;

namespace Fhi.Authentication.Extensions.UnitTests.Setup;

public static class HttpClientFactoryCreator
{
    public static IHttpClientFactory CreateFactory()
    {
        var services = new ServiceCollection();

        services.AddHttpClient();

        var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IHttpClientFactory>();
    }
}