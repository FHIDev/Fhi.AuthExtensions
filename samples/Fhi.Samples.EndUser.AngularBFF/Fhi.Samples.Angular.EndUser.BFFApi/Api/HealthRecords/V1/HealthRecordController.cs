using AngularBFF.Net8.Api.HealthRecords.V1.Dto;
using Microsoft.AspNetCore.Mvc;

namespace AngularBFF.Net8.Api.HealthRecords.V1
{
  [ApiController]
  [Route("/bff/v1/health-records")]
  public class HealthRecordController(IHealthRecordService weatherService, ILogger<HealthRecordController> logger) : ControllerBase
  {
    private const string DownstreamUrl = "https://localhost:7150";
    private const string StartCommand = "cd samples/Fhi.Samples.WebApi && dotnet run --launch-profile https";

    public IHealthRecordService Service { get; } = weatherService;

    [HttpGet]
    public async Task<IActionResult> Get()
    {
      try
      {
        var records = await Service.GetRecords();
        return Ok(records?.Select(r => new HealthRecordDto() { Name = r.Name, Description = r.Description, CreatedAt = r.CreatedAt }));
      }
      catch (UnauthorizedAccessException ex)
      {
        logger.LogWarning(ex, "Downstream WebApi at {Url} returned 401 (token rejected or refresh failed)", DownstreamUrl);
        return Problem(
            title: "Downstream WebApi rejected the access token",
            detail: $"The downstream WebApi at {DownstreamUrl} returned 401.",
            statusCode: StatusCodes.Status502BadGateway);
      }
      catch (TaskCanceledException ex) when (!HttpContext.RequestAborted.IsCancellationRequested)
      {
        logger.LogWarning(ex, "Downstream WebApi at {Url} timed out", DownstreamUrl);
        return DownstreamUnreachable("timed out");
      }
      catch (HttpRequestException ex) when (ex.StatusCode is null)
      {
        logger.LogWarning(ex, "Downstream WebApi at {Url} unreachable", DownstreamUrl);
        return DownstreamUnreachable(ex.Message);
      }
    }

    private ObjectResult DownstreamUnreachable(string reason) =>
        Problem(
            title: "Downstream sample WebApi is unreachable",
            detail: $"The BFF could not reach the sample WebApi at {DownstreamUrl} ({reason}). " +
                    $"It probably isn't running. Start it with: {StartCommand}",
            statusCode: StatusCodes.Status503ServiceUnavailable);
  }
}
