namespace WorkerService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public Worker(ILogger<Worker> logger, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var client = _httpClientFactory.CreateClient("api");

            while (!stoppingToken.IsCancellationRequested)
            {
                var response = await client.GetAsync("/weatherforecast");
                var req = response.RequestMessage;
                if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                    _logger.LogInformation("Request: {req}", req);
                }
                await Task.Delay(10000, stoppingToken);
            }
        }
    }
}
