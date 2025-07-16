using Fhi.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.Services;

namespace WebApi.Api.HealthRecord.Integration.v1
{
    public record HealthRecordDto(string Name, string Description, DateTime CreatedAt);

    [ApiController]
    [Route("api/v1/integration/health-records")]
    [Authorize(AuthenticationSchemes = "bearer.integration", Policy = "IntegrationPolicy")]
    [Scope("api")]
    public class HealthRecordController(IHealthRecordService healthRecordService) : ControllerBase
    {
        private readonly IHealthRecordService _healthRecordService = healthRecordService;

        [HttpGet]
        public IEnumerable<HealthRecordDto> Get()
        {
            return _healthRecordService.GetHealthRecords().Select(r => new HealthRecordDto(r.Name, r.Description, r.CreatedAt));
        }
    }
}
