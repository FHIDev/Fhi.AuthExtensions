using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Fhi.Authentication.JwtDPoP.Tests.Setup
{
    internal class DPoPTestServerBuilder
    {
        private Action<IServiceCollection> _configureServices = _ => { };
        private Action<WebApplication> _configureEndpoints = _ => { };


        internal DPoPTestServerBuilder AddServiceConfiguration(Action<IServiceCollection> configure)
        {
            _configureServices = configure;
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
                    _configureServices.Invoke(services);
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
