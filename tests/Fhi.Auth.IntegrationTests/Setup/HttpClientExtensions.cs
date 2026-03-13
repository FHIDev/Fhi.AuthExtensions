using Fhi.Authentication.JwtDPoP;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Net.Http.Headers;

namespace Fhi.Auth.IntegrationTests.Setup
{
    internal static class HttpClientExtensions
    {
        internal static HttpClient AddDPoPAuthorizationHeader(this HttpClient client, string token)
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("DPoP", token);
            return client;
        }

        internal static HttpClient AddBearerAuthorizationHeader(this HttpClient client, string token)
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        internal static HttpClient AddDPoPHeader(this HttpClient client, string proof)
        {
            client.DefaultRequestHeaders.Add("DPoP", proof);
            return client;
        }
    }

    internal class TestAuthenticationBuilder(AuthenticationBuilder builder)
    {
        internal TestAuthenticationBuilder AddJwtDpop(
            string authority = "http://authority",
            string audience = "api_audience",
            Action<JwtDPoPOptions>? configure = null)
        {
            builder.AddJwtDpop("DPoP", options =>
            {
                options.Authority = authority;
                options.Audience = audience;
                options.RequireHttpsMetadata = false;
                configure?.Invoke(options);
            });
            return this;
        }

        internal TestAuthenticationBuilder AddJwtBearer(
            string scheme = "Bearer",
            string authority = "http://authority",
            string audience = "api_audience",
            Action<JwtBearerOptions>? configure = null)
        {
            builder.AddJwtBearer(scheme, options =>
            {
                options.Authority = authority;
                options.Audience = audience;
                options.RequireHttpsMetadata = false;
                configure?.Invoke(options);
            });
            return this;
        }
    }

    internal class DPoPTestServerBuilder
    {
        private Action<TestAuthenticationBuilder> _configureAuth = _ => { };
        private Action<WebApplication> _configureEndpoints = _ => { };

        internal DPoPTestServerBuilder AddServiceConfiguration(Action<TestAuthenticationBuilder> configure)
        {
            _configureAuth = configure;
            return this;
        }

        internal DPoPTestServerBuilder AppPipeline(Action<WebApplication> configureEndpoints)
        {
            _configureEndpoints = configureEndpoints;
            return this;
        }

        internal HttpClient Start()
        {
            var builder = WebApplicationBuilderTestHost
                .CreateWebHostBuilder()
                .WithServices((services, _) =>
                {
                    var authBuilder = services.AddAuthentication();
                    _configureAuth(new TestAuthenticationBuilder(authBuilder));
                    services.AddAuthorization();
                });

            var app = builder.BuildApp(app =>
            {
                app.UseRouting().UseAuthentication().UseAuthorization();
                _configureEndpoints(app);
            });
            app.Start();

            return app.GetTestClient();
        }
    }
}
