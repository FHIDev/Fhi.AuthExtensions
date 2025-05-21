using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Refit;

namespace Fhi.Authentication.Extensions.UnitTests;

[TestFixture]
public class ServiceCollectionExtensionsRefitTests
{
    private IServiceCollection _services;
    private IConfiguration _configuration;

    public interface ITestApi
    {
        [Get("/test")]
        Task<string> GetTestAsync();
    }

    [SetUp]
    public void SetUp()
    {
        _services = new ServiceCollection();

        var configurationData = new Dictionary<string, string?>
        {
            ["RefitClientCredentials:TokenEndpoint"] = "https://auth.example.com/token",
            ["RefitClientCredentials:ClientId"] = "test-client-id",
            ["RefitClientCredentials:ClientSecret"] = "test-client-secret",
            ["RefitClientCredentials:Scope"] = "api.read",
            ["RefitClientCredentials:ApiBaseUrl"] = "https://api.example.com",
            ["RefitClientCredentials:ClientName"] = "test-client"
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configurationData)
            .Build();
    }

    [Test]
    public void AddRefitClientWithClientCredentials_ShouldRegisterServices()
    {
        _services.AddRefitClientWithClientCredentials<ITestApi>(_configuration);
        var serviceProvider = _services.BuildServiceProvider();

        var refitClient = serviceProvider.GetService<ITestApi>();
        Assert.That(refitClient, Is.Not.Null);
    }

    [Test]
    public void AddRefitClientWithClientCredentials_ShouldThrowWhenConfigurationMissing()
    {
        var emptyConfiguration = new ConfigurationBuilder().Build();

        Assert.Throws<InvalidOperationException>(() =>
            _services.AddRefitClientWithClientCredentials<ITestApi>(emptyConfiguration));
    }

    [Test]
    public void AddRefitClientWithClientCredentials_ShouldThrowWhenTokenEndpointMissing()
    {
        var configurationData = new Dictionary<string, string?>
        {
            ["RefitClientCredentials:ClientId"] = "test-client-id"
            // TokenEndpoint is missing
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configurationData)
            .Build();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            _services.AddRefitClientWithClientCredentials<ITestApi>(configuration));

        Assert.That(exception.Message, Does.Contain("TokenEndpoint"));
    }

    [Test]
    public void AddRefitClientWithClientCredentials_ShouldThrowWhenClientIdMissing()
    {
        var configurationData = new Dictionary<string, string?>
        {
            ["RefitClientCredentials:TokenEndpoint"] = "https://auth.example.com/token"
            // ClientId is missing
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configurationData)
            .Build();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            _services.AddRefitClientWithClientCredentials<ITestApi>(configuration));

        Assert.That(exception.Message, Does.Contain("ClientId"));
    }

    [Test]
    public void AddRefitClientWithClientCredentials_ShouldReturnHttpClientBuilder()
    {
        var httpClientBuilder = _services.AddRefitClientWithClientCredentials<ITestApi>(_configuration);

        Assert.That(httpClientBuilder, Is.Not.Null);
        Assert.That(httpClientBuilder, Is.InstanceOf<IHttpClientBuilder>());
    }

    [Test]
    public void AddRefitClientWithClientCredentials_ShouldAllowRefitSettingsConfiguration()
    {
        var refitSettingsCalled = false;

        _services.AddRefitClientWithClientCredentials<ITestApi>(_configuration,
            refitSettings =>
            {
                refitSettingsCalled = true;
                // Configure settings if needed
            });

        Assert.That(refitSettingsCalled, Is.True);
    }

    [Test]
    public void AddRefitClientWithClientCredentials_ShouldAllowHttpClientConfiguration()
    {
        var httpClientCalled = false;

        _services.AddRefitClientWithClientCredentials<ITestApi>(_configuration,
            configureHttpClient: httpClient =>
            {
                httpClientCalled = true;
                httpClient.Timeout = TimeSpan.FromSeconds(30); // Set a test configuration
            });

        // Build the service provider and resolve the service to trigger the callback
        using var serviceProvider = _services.BuildServiceProvider();
        var api = serviceProvider.GetService<ITestApi>();

        Assert.That(httpClientCalled, Is.True);
        Assert.That(api, Is.Not.Null);
    }
}