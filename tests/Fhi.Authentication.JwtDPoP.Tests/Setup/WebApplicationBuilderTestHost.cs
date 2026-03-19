using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Fhi.Authentication.JwtDPoP.Tests.Setup
{
    internal static class WebApplicationBuilderTestHost
    {
        internal static WebApplicationBuilder CreateWebHostBuilder()
        {
            var builder = WebApplication.CreateBuilder([]);
            builder.WebHost.UseTestServer();
            return builder;
        }

        internal static WebApplicationBuilder WithServices(
         this WebApplicationBuilder builder,
         Action<IServiceCollection, IConfiguration> configure)
        {
            configure?.Invoke(builder.Services, builder.Configuration);
            return builder;
        }
        internal static WebApplication BuildApp(this WebApplicationBuilder builder, Action<WebApplication> appBuilder)
        {
            var app = builder.Build();
            appBuilder.Invoke(app);
            return app;
        }
    }
}
