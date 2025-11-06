using Fhi.Auth.IntegrationTests.Setup;
using Fhi.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Net;

namespace Fhi.Auth.IntegrationTests
{
    public partial class Tests
    {
        /// <summary>
        /// Verify scope authorization works as expected when endpoint has scope attribute.
        /// </summary>
        /// <returns></returns>
        [Test]
        public async Task EndpointWithScopeAuthorization()
        {
            FakeAuthHandler.TestClaims =
            [
                new System.Security.Claims.Claim("scope", "fhi:webapi/health-records.read"),
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, "Test User")
            ];

            var builder = WebApplicationBuilderTestHost
                .CreateWebHostBuilder()
                .WithServices((services, _) =>
                {
                    services.AddAuthentication("Fake")
                        .AddScheme<AuthenticationSchemeOptions, FakeAuthHandler>("Fake", options => { });
                    services.AddSingleton<IAuthorizationHandler, ScopeHandler>();
                    services.AddAuthorization();
                });

            var app = builder.BuildApp(app =>
            {
                app.UseRouting();
                app.UseAuthentication();
                app.UseAuthorization();
                app.MapGet("/api/access-ok", [Scope("fhi:webapi/health-records.read")] async (context) =>
                {
                    await context.Response.WriteAsync($"Successful access");
                });
                app.MapGet("/api/access-denied", [Scope("fhi:webapi/health-records.write")] async (context) =>
                {
                    await context.Response.WriteAsync($"Successful access");
                });
            });
            app.Start();

            var client = app.GetTestClient();

            var response = await client.GetAsync("/api/access-ok");
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            var responseAccessDenied = await client.GetAsync("/api/access-denied");
            Assert.That(responseAccessDenied.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
        }
    }
}
