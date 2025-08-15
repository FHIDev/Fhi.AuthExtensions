using Fhi.Samples.WebApi.Api.HealthRecord.Me.v1.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.Services;

namespace WebApi.Api.HealthRecord.Me.v1
{

    [ApiController]
    [Route("api/v1/me/health-records")]
    [Authorize(AuthenticationSchemes = "bearer.me", Policy = "EndUserPolicy")]
    public class HealthRecordController(IHealthRecordService healthRecordService) : ControllerBase
    {
        private readonly IHealthRecordService _healthRecordService = healthRecordService;

        [HttpGet]
        public IEnumerable<HealthRecordPersonDto> Get()
        {
            Task.Delay(1000).Wait();
            return _healthRecordService.GetHealthRecords().Select(r => new HealthRecordPersonDto(r.Pid, r.Name, r.Description, r.CreatedAt));
        }
    }
}
