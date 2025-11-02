using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Fhi.Auth.IntegrationTests.Setup
{
    internal static class WebApplicationBuilderTestHost
    {
        internal static WebApplicationBuilder CreateWebHostBuilder()
        {
            var builder = WebApplication.CreateBuilder([]);
            builder.WebHost.UseTestServer();
            return builder;
        }

        internal static WebApplicationBuilder WithUserSecrets(this WebApplicationBuilder builder, string usersecretId)
        {
            var config = new ConfigurationBuilder()
                        .AddUserSecrets(usersecretId)
                        .Build();
            builder.Configuration.AddConfiguration(config);
            return builder;
        }

        internal static WebApplicationBuilder WithAppSettings(this WebApplicationBuilder builder, string fileName)
        {
            var config = new ConfigurationBuilder()
                        .SetBasePath(AppContext.BaseDirectory)
                        .AddJsonFile(fileName, optional: false, reloadOnChange: false)
                        .Build();
            builder.Configuration.AddConfiguration(config);
            return builder;
        }

        internal static WebApplicationBuilder WithUrls(this WebApplicationBuilder builder, params string[] urls)
        {
            builder.WebHost.UseUrls(urls);
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
