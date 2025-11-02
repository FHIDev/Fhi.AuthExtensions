using Duende.AccessTokenManagement;
using Fhi.Auth.IntegrationTests.Setup;
using Fhi.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Fhi.Auth.EndToEndTests.Authentication
{
    /// <summary>
    /// Manual acceptance test for Client Credentials authentication against HelseId test environment.
    /// </summary>
    [TestFixture, Explicit("Test that runs against HelseId test environment")]
    public class ClientCredentialsAuthenticationTests
    {
        [Test, Explicit]
        public async Task HelseIdClientTest()
        {
            /**************************************************************************************************
             * Setup Client application Host with Client Credentials configuration for HttpClient that is calling API
             * ************************************************************************************************/
            var clientBuilder = WebApplicationBuilderTestHost
               .CreateWebHostBuilder()
               .WithUrls("http://localhost:7777")
               .WithUserSecrets("ffb38d7f-f087-47e4-ba0e-4a8d7ec56be6")
               //.WithAppSettings("Authentication\\appsettings.ClientCredentialsTests.json")
               .WithServices((services, configuration) =>
               {
                   var authority = configuration.GetValue<string>("HelseIdClient:Authority");
                   services.AddInMemoryDiscoveryService([authority!]);
                   var privateJwk = configuration.GetValue<string>("HelseIdClient:PrivateJwkJsonEscaped");
                   var clientCredentialsOption = services
                   .AddClientCredentialsClientOptions(
                       "Name",
                       authority!,
                       configuration.GetValue<string>("HelseIdClient:ClientId")!,
                       PrivateJwk.ParseFromJson(privateJwk!),
                       configuration.GetValue<string>("HelseIdClient:Scope")!)
                   // With DPoP support
                   ////DPoPProofKey.ParseOrDefault(PrivateJwk.ParseFromJson(privateJwk!)))
                   .AddClientCredentialsHttpClient(client =>
                   {
                       client.BaseAddress = new Uri("http://localhost:8888");
                   });

                   services.AddClientCredentialsTokenManagement();
                   services.AddSingleton<MyService>();
               });
            var clientApp = clientBuilder.BuildApp(app => { });

            /**************************************************************************************************
            * Setup Api application Host with an API endpoint
            * ************************************************************************************************/
            var apiApp = CreateWebApplication("http://localhost:8888");
            apiApp.MapGet("/api/v1/tests", async (context) =>
            {
                context.Request.Headers.TryGetValue("Authorization", out var authHeader);
                await context.Response.WriteAsync($"{authHeader}");
            });

            /**************************************************************************************************
            * Start both applications and perform the test call from Client to API
            * ************************************************************************************************/
            await clientApp.StartAsync();
            await apiApp.StartAsync();

            var response = await clientApp.Services.GetRequiredService<MyService>().Get();
            var content = await response.Content.ReadAsStringAsync();
            Assert.That(response.IsSuccessStatusCode, Is.True);
            Assert.That(content, Does.StartWith("Bearer "));

            await clientApp.StopAsync();
            await apiApp.StopAsync();
        }

        private static WebApplication CreateWebApplication(params string[] urls)
        {
            var apiBuilder = WebApplication
                .CreateBuilder([]);
            apiBuilder.WebHost.UseUrls(urls);
            return apiBuilder.Build();
        }

        class MyService
        {
            private readonly IHttpClientFactory _httpClientFactory;
            public MyService(IHttpClientFactory httpClientFactory)
            {
                _httpClientFactory = httpClientFactory;
            }
            public async Task<HttpResponseMessage> Get()
            {
                var client = _httpClientFactory.CreateClient("Name");
                return await client.GetAsync("/api/v1/tests");
            }
        }
    }
}
