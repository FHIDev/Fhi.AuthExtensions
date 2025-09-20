using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;

namespace Fhi.Auth.IntegrationTests.Setup
{
    internal static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddFakeLogProvider(this IServiceCollection services, FakeLoggerProvider fakeLogProvider, LogLevel logLevel = LogLevel.Debug)
        {
            services.AddLogging();
            var factory = CreateLogFactory(fakeLogProvider, logLevel);
            services.AddSingleton(factory);
            services.AddSingleton(factory.CreateLogger("general logs"));

            return services;
        }

        private static ILoggerFactory CreateLogFactory(ILoggerProvider logProvider, LogLevel logLevel)
        {
            return LoggerFactory.Create(loggerBuilder =>
            {
                loggerBuilder.AddProvider(logProvider);
                loggerBuilder.SetMinimumLevel(logLevel);
            });
        }
    }
}
