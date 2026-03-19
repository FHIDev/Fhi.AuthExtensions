using Microsoft.Extensions.Hosting;

namespace M2M.Host.CertificateSecret
{
    internal class BackgroundServiceCallingAPI(
        IHttpClientFactory httpClientFactory) : BackgroundService
    {
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var client = httpClientFactory.CreateClient(ApiOption.ClientName);
            var response = client.GetAsync("api/v1/tests", stoppingToken);

            return Task.CompletedTask;
        }
    }
}
