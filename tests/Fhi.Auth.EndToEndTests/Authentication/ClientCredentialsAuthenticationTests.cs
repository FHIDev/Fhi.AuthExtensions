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
               .WithAppSettings("Authentication\\appsettings.ClientCredentialsTests.json")
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
                   .AddClientCredentialsHttpClient(client =>
                    {
                        client.BaseAddress = new Uri("http://localhost:8888");
                    });

                   services.AddClientCredentialsTokenManagement();


                   services.AddSingleton<MyService>();
               });

            var clientApp = clientBuilder.BuildApp(app => { });
            await clientApp.StartAsync();

            /**************************************************************************************************
             * Setup Api application Host with an API endpoint
             * ************************************************************************************************/

            var apiBuilder = WebApplication.CreateBuilder([]);
            apiBuilder.WebHost.UseUrls("http://localhost:8888");
            var api = apiBuilder.Build();
            api.MapGet("/api/v1/tests", async (context) =>
            {
                context.Request.Headers.TryGetValue("Authorization", out var authHeader);
                await context.Response.WriteAsync($"{authHeader}");
            });
            await api.StartAsync();


            var response = await clientApp.Services.GetRequiredService<MyService>().Get();
            var content = await response.Content.ReadAsStringAsync();
            Assert.That(response.IsSuccessStatusCode, Is.True);
            Assert.That(content, Does.StartWith("Bearer "));
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
