using Fhi.Auth.IntegrationTests.Setup;
using Fhi.Authentication;
using Fhi.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Testing;
using NUnit.Framework.Internal;
using System.Net;
using System.Text.Json;

namespace Fhi.Auth.IntegrationTests
{
    /// <summary>
    /// Tests for InMemoryDiscoveryDocument service using AddInMemoryDiscoveryService extension method.
    /// Sample of register service: 
    /// <code>
    /// services.AddInMemoryDiscoveryService(
    ///  [
    ///     new DiscoveryDocumentStoreOptions() { Authority = "https://xxx", CacheDuration = TimeSpan.FromDays(2) }
    ///  ]);
    /// </code>
    /// </summary>
    public class InMemoryDiscoveryDocumentTests
    {
        [Test]
        public void GIVEN_AddInMemoryDiscoveryService_WHEN_authority_invalid_url_THEN_throwExceptionOnStartup()
        {
            var authority = "invalid";
            var (app, _) = new InMemoryDiscoveryDocumentServiceTestBuilder()
                .WithAuthority(authority)
                .WithEndpoint(app => app.MapGet("api/discovery-test/{authority}", (string authority, IDiscoveryDocumentStore store) =>
                    Results.Ok(store.Get(Uri.UnescapeDataString(authority)))))
                .Build();

            var client = app.GetTestClient();
            var ex = Assert.ThrowsAsync<InvalidOperationException>(async () => await client.GetAsync($"/api/discovery-test/{Uri.EscapeDataString(authority)}"));
            Assert.That(ex.Message, Is.EqualTo("Malformed URL"));
        }

        [Test]
        public async Task GIVEN_AddInMemoryDiscoveryService_WHEN_connection_refused_THEN_log_error()
        {
            var authority = "http://localhost";
            var (app, fakeLogProvider) = new InMemoryDiscoveryDocumentServiceTestBuilder()
              .WithAuthority(authority)
              .WithFakeLogging()
              .WithEndpoint(app => app.MapGet("api/discovery-test/{authority}", (string authority, IDiscoveryDocumentStore store) =>
                    Results.Ok(store.Get(Uri.UnescapeDataString(authority)))))
              .Build();

            var client = app.GetTestClient();

            var response = await client.GetAsync($"/api/discovery-test/{Uri.EscapeDataString($"{authority}")}");
            var errorLog = fakeLogProvider?.Collector?.GetSnapshot().FirstOrDefault(x => x.Level == Microsoft.Extensions.Logging.LogLevel.Error);
            Assert.That(errorLog?.Message, Contains.Substring("Could not load Discovery document for Authority"));
        }

        [Test]
        public async Task GIVEN_AddInMemoryDiscoveryService_WHEN_valid_authority_THEN_loadDocument_getDocuments()
        {
            var authority = "https://helseid-sts.test.nhn.no/";
            var (app, _) = new InMemoryDiscoveryDocumentServiceTestBuilder()
              .WithAuthority(authority)
              .WithEndpoint(app => app.MapGet("api/discovery-test/{authority}", (string authority, IDiscoveryDocumentStore store) =>
                    Results.Ok(store.Get(Uri.UnescapeDataString(authority)))))
              .Build();

            var client = app.GetTestClient();
            var response = await client.GetAsync($"/api/discovery-test/{Uri.EscapeDataString(authority)}");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            var document = (await response.Content.ReadAsStringAsync()).Deserialize<DiscoveryDocument>();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(document!.Issuer, Is.EqualTo("https://helseid-sts.test.nhn.no"));
                Assert.That(document!.Authority, Is.EqualTo(authority));
                Assert.That(document!.UserInfoEndpoint, Is.EqualTo("https://helseid-sts.test.nhn.no/connect/userinfo"));
                Assert.That(document!.JwksUri, Is.EqualTo("https://helseid-sts.test.nhn.no/.well-known/openid-configuration/jwks"));
            }
        }

        [Test]
        public async Task GIVEN_AddInMemoryDiscoveryService_WHEN_multiple_authorities_THEN_loadAllDocuments()
        {
            var helseIdAuthority = "https://helseid-sts.test.nhn.no/";
            var demoAuthority = "https://demo.duendesoftware.com";

            var (app, _) = new InMemoryDiscoveryDocumentServiceTestBuilder()
                .WithAuthority(helseIdAuthority)
                .WithAuthority(demoAuthority)
                .WithEndpoint(app =>
                {
                    app.MapGet("/api/discovery-test", ([FromQuery] string[] authority, IDiscoveryDocumentStore store) =>
                    Results.Ok(authority.Select(auth => store.Get(Uri.UnescapeDataString(auth)))));
                })
                .Build();

            var client = app.GetTestClient();
            var helseIdAuthorityResponse = await client.GetAsync($"/api/discovery-test?authority={Uri.EscapeDataString(helseIdAuthority)}&authority={Uri.EscapeDataString(demoAuthority)}");

            Assert.That(helseIdAuthorityResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            var documents = (await helseIdAuthorityResponse.Content.ReadAsStringAsync()).Deserialize<List<DiscoveryDocument>>();
            Assert.That(documents!, Has.Count.EqualTo(2));

            var helseIdDocument = documents.FirstOrDefault(d => d.Authority == helseIdAuthority);
            Assert.That(helseIdDocument!.Issuer, Is.EqualTo("https://helseid-sts.test.nhn.no"));
            Assert.That(helseIdDocument!.Authority, Is.EqualTo(helseIdAuthority));

            var demoDocument = documents.FirstOrDefault(d => d.Authority == demoAuthority);
            Assert.That(demoDocument!.Issuer, Is.EqualTo("https://demo.duendesoftware.com"));
            Assert.That(demoDocument!.Authority, Is.EqualTo(demoAuthority));
        }

        [Test]
        public async Task GIVEN_AddInMemoryDiscoveryService_WHEN_cacheEntryExpired_THEN_loadNewDocumentIntoCache()
        {
            var authority = "https://helseid-sts.test.nhn.no/";
            var (app, fakeLogProvider) = new InMemoryDiscoveryDocumentServiceTestBuilder()
               .WithAuthority(authority, TimeSpan.FromSeconds(1))
               .WithFakeLogging()
               .WithEndpoint(app =>
               {
                   app.MapGet("/api/discovery-test", ([FromQuery] string[] authority, IDiscoveryDocumentStore store) =>
                   Results.Ok(authority.Select(auth => store.Get(Uri.UnescapeDataString(auth)))));
               })
               .Build();

            var client = app.GetTestClient();

            var firstRequest = await client.GetAsync($"/api/discovery-test?authority={Uri.EscapeDataString(authority)}");
            Assert.That(firstRequest.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            var logsFirst = fakeLogProvider?.Collector?.GetSnapshot().Where(x => x.Message.Contains(authority)).ToList();
            Assert.That(logsFirst, Has.Count.EqualTo(4), "Expected log entry for loading the discovery document");

            var secondRequest = await client.GetAsync($"/api/discovery-test?authority={Uri.EscapeDataString(authority)}");
            Assert.That(secondRequest.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            var logsSecond = fakeLogProvider?.Collector?.GetSnapshot().Where(x => x.Message.Contains(authority)).ToList();
            Assert.That(logsSecond, Has.Count.EqualTo(4), "Expected unchanged log entry since document should be loaded from cahce");

            await Task.Delay(TimeSpan.FromSeconds(2)); // Wait for cache to expire

            var thirdRequest = await client.GetAsync($"/api/discovery-test?authority={Uri.EscapeDataString(authority)}");
            Assert.That(thirdRequest.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            var logsThird = fakeLogProvider?.Collector?.GetSnapshot().Where(x => x.Message.Contains(authority)).ToList();
            Assert.That(logsThird, Has.Count.EqualTo(8), "Expected empty cahce and document should be loaded from cahce");
        }
    }

    internal class InMemoryDiscoveryDocumentServiceTestBuilder
    {
        private readonly WebApplicationBuilder _builder;
        private readonly List<DiscoveryDocumentStoreOptions> _authorities = [];
        private FakeLoggerProvider? _logProvider;
        private readonly List<Action<WebApplication>> _endpoints = [];

        public InMemoryDiscoveryDocumentServiceTestBuilder()
        {
            _builder = WebApplicationBuilderTestHost.CreateWebHostBuilder();
        }

        public InMemoryDiscoveryDocumentServiceTestBuilder WithAuthority(string authority, TimeSpan? cacheDuration = null)
        {
            _authorities.Add(new DiscoveryDocumentStoreOptions
            {
                Authority = authority,
                CacheDuration = cacheDuration ?? TimeSpan.FromMinutes(60)
            });
            return this;
        }

        public InMemoryDiscoveryDocumentServiceTestBuilder WithFakeLogging()
        {
            _logProvider = new FakeLoggerProvider();
            return this;
        }
        public InMemoryDiscoveryDocumentServiceTestBuilder WithEndpoint(Action<WebApplication> endpointConfig)
        {
            _endpoints.Add(endpointConfig);
            return this;
        }

        public (WebApplication App, FakeLoggerProvider? LogProvider) Build()
        {
            if (_logProvider != null)
            {
                _builder.Services.AddFakeLogProvider(_logProvider);
            }

            _builder.Services.AddInMemoryDiscoveryService(_authorities.ToArray());

            var app = _builder.BuildApp(app =>
            {
                app.UseRouting();
                foreach (var endpoint in _endpoints)
                {
                    endpoint(app);
                }
            });

            app.Start();
            return (app, _logProvider);
        }
    }
}
