using Microsoft.Extensions.Options;
using Fhi.Samples.RefitClientCredentials.Services;

namespace Fhi.Samples.RefitClientCredentials.Workers;

/// <summary>
/// Background service that periodically calls the Weather API using client credentials authentication.
/// This demonstrates how to use the Refit client with automatic token management.
/// </summary>
public class HealthRecordsWorker : BackgroundService
{
    private readonly ILogger<HealthRecordsWorker> _logger;
    private readonly IHealthRecordsApi _weatherApi;
    private readonly WorkerServiceOptions _options;

    public HealthRecordsWorker(
        ILogger<HealthRecordsWorker> logger,
        IHealthRecordsApi weatherApi,
        IOptions<WorkerServiceOptions> options)
    {
        _logger = logger;
        _weatherApi = weatherApi;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Weather Worker running at: {time}", DateTimeOffset.Now);

            try
            {
                // Call the Weather API using client credentials authentication
                _logger.LogInformation("Calling Weather API...");
                var weatherForecasts = await _weatherApi.GetClients();

                var person = await _weatherApi.GetPersonById(new PersonRequest
                {
                    Fnr = "01010101011"
                });

                // if (weatherForecasts != null && weatherForecasts.Any())
                // {
                //     _logger.LogInformation("Successfully retrieved {Count} weather forecasts:", weatherForecasts.Count());
                //     foreach (var forecast in weatherForecasts)
                //     {
                //         _logger.LogInformation(
                //             "Weather Forecast - Date: {Date}, Temperature: {TemperatureC}°C ({TemperatureF}°F), Summary: {Summary}",
                //             forecast.Date.ToString("yyyy-MM-dd"), forecast.TemperatureC, forecast.TemperatureF, forecast.Summary);
                //     }
                // }
                // else
                // {
                //     _logger.LogInformation("No weather forecasts found.");
                // }
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError(httpEx, "HTTP error calling Weather API. Status: {StatusCode}",
                    httpEx.Data.Contains("StatusCode") ? httpEx.Data["StatusCode"] : "Unknown");
            }
            catch (UnauthorizedAccessException authEx)
            {
                _logger.LogError(authEx, "Unauthorized access to Weather API. Check client credentials and scopes.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error calling Weather API");
            }

            _logger.LogInformation("Waiting {DelaySeconds} seconds before next call...", _options.DelaySeconds);
            await Task.Delay(TimeSpan.FromSeconds(_options.DelaySeconds), stoppingToken);
        }
    }
}

public class WorkerServiceOptions
{
    public int DelaySeconds { get; set; } = 5; // Default delay
}
