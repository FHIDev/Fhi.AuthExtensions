using Api.WebApi.Hosting;
using Fhi.Samples.WebApi.Api.HealthRecord.Me.v1.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.Services;

namespace WebApi.Api.HealthRecord.Me.v1
{

    [ApiController]
    [Route("api/v1/me/health-records")]

    public class HealthRecordController(IHealthRecordService healthRecordService) : ControllerBase
    {
        private readonly IHealthRecordService _healthRecordService = healthRecordService;

        [HttpGet]
        [Authorize(AuthenticationSchemes = $"{AuthenticationSchemes.HelseIdDPoP},{AuthenticationSchemes.Duende}", Policy = Policies.EndUserPolicy)]
        public IEnumerable<HealthRecordPersonDto> GetWithHelseIdBearer()
        {
            return _healthRecordService.GetHealthRecords().Select(r => new HealthRecordPersonDto(r.Pid, r.Name, r.Description, r.CreatedAt));
        }
    }
}
