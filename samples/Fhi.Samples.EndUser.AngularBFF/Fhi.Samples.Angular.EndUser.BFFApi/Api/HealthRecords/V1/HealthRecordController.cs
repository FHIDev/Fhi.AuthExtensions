using AngularBFF.Net8.Api.HealthRecords.V1.Dto;
using Microsoft.AspNetCore.Mvc;

namespace AngularBFF.Net8.Api.HealthRecords.V1
{
    [ApiController]
    [Route("/bff/v1/health-records")]
    public class HealthRecordController(IHealthRecordService weatherService) : ControllerBase
    {
        public IHealthRecordService Service { get; } = weatherService;

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            try
            {
                var records = await Service.GetRecords();
                return Ok(records?.Select(r => new HealthRecordDto() { Name = r.Name, Description = r.Description, CreatedAt = r.CreatedAt }));

            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized();
            }

        }
    }
}
