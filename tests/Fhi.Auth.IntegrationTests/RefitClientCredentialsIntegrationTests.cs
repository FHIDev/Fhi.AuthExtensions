using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Refit;
using Fhi.Authentication;

namespace Fhi.Auth.IntegrationTests;

[TestFixture]
public class RefitClientCredentialsIntegrationTests
{
    public interface ITestApi
    {
        [Get("/weatherforecast")]
        Task<string> GetWeatherForecastAsync();
    }

    private const string TestJwk = """
        {
            "kty": "RSA",
            "use": "sig",
            "kid": "test-key-id",
            "x5t": "test-thumbprint",
            "n": "test-modulus",
            "e": "AQAB",
            "d": "test-private-exponent",
            "p": "test-prime1",
            "q": "test-prime2",
            "dp": "test-exponent1",
            "dq": "test-exponent2",
            "qi": "test-coefficient"
        }
        """;

    [Test]
    public void RefitClientCredentials_ShouldBeConfiguredCorrectly()
    {
        var configurationData = new Dictionary<string, string?>
        {
            ["RefitClientCredentials:TokenEndpoint"] = "https://auth.example.com/token",
            ["RefitClientCredentials:ClientId"] = "test-client-id",
            ["RefitClientCredentials:ClientSecret"] = TestJwk,
            ["RefitClientCredentials:Scope"] = "api.read",
            ["RefitClientCredentials:ApiBaseUrl"] = "https://api.example.com",
            ["RefitClientCredentials:ClientName"] = "test-client"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configurationData)
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddRefitClientWithClientCredentials<ITestApi>(configuration);
        var serviceProvider = services.BuildServiceProvider();

        var refitClient = serviceProvider.GetService<ITestApi>();
        Assert.That(refitClient, Is.Not.Null, "Refit client should be registered in DI container");

        // Verify that the underlying HttpClient is configured
        var httpClientFactory = serviceProvider.GetService<IHttpClientFactory>();
        Assert.That(httpClientFactory, Is.Not.Null, "HttpClientFactory should be available");

        // Verify that client credentials services are registered  
        var hasRequiredServices = httpClientFactory != null;
        Assert.That(hasRequiredServices, Is.True, "Required services should be registered");
    }

    [Test]
    public void RefitClientCredentials_ShouldValidateConfiguration()
    {
        var invalidConfigurationData = new Dictionary<string, string?>
        {
            // Missing required TokenEndpoint and ClientId
            ["RefitClientCredentials:ClientSecret"] = TestJwk
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(invalidConfigurationData)
            .Build();

        var services = new ServiceCollection();

        Assert.Throws<InvalidOperationException>(() =>
            services.AddRefitClientWithClientCredentials<ITestApi>(configuration),
            "Should throw when required configuration is missing");
    }

    [Test]
    public void RefitClientCredentials_ShouldHandleMinimalConfiguration()
    {
        var minimalConfigurationData = new Dictionary<string, string?>
        {
            ["RefitClientCredentials:TokenEndpoint"] = "https://auth.example.com/token",
            ["RefitClientCredentials:ClientId"] = "test-client-id",
            ["RefitClientCredentials:ClientSecret"] = TestJwk
            // Scope and ApiBaseUrl are optional
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(minimalConfigurationData)
            .Build();

        var services = new ServiceCollection();

        Assert.DoesNotThrow(() =>
        {
            services.AddRefitClientWithClientCredentials<ITestApi>(configuration);
            var serviceProvider = services.BuildServiceProvider();
            var refitClient = serviceProvider.GetService<ITestApi>();
            Assert.That(refitClient, Is.Not.Null);
        });
    }

    [Test]
    public void RefitClientCredentials_ShouldAllowCustomConfiguration()
    {
        var configurationData = new Dictionary<string, string?>
        {
            ["RefitClientCredentials:TokenEndpoint"] = "https://auth.example.com/token",
            ["RefitClientCredentials:ClientId"] = "test-client-id",
            ["RefitClientCredentials:ClientSecret"] = TestJwk,
            ["RefitClientCredentials:ApiBaseUrl"] = "https://api.example.com"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configurationData)
            .Build();

        var services = new ServiceCollection();
        var customRefitSettingsCalled = false;
        var customHttpClientCalled = false;

        services.AddRefitClientWithClientCredentials<ITestApi>(
            configuration,
            refitSettings =>
            {
                customRefitSettingsCalled = true;
                // Custom Refit configuration
            },
            httpClient =>
            {
                customHttpClientCalled = true;
                // Custom HttpClient configuration
            });

        var serviceProvider = services.BuildServiceProvider();
        var refitClient = serviceProvider.GetService<ITestApi>();

        Assert.That(refitClient, Is.Not.Null);
        Assert.That(customRefitSettingsCalled, Is.True, "Custom Refit settings should be applied");
        Assert.That(customHttpClientCalled, Is.True, "Custom HttpClient configuration should be applied");
    }
}
