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

        [HttpGet("helseid-bearer")]
        [Authorize(AuthenticationSchemes = AuthenticationSchemes.HelseIdBearer, Policy = Policies.EndUserPolicy)]
        public IEnumerable<HealthRecordPersonDto> GetWithHelseIdBearer()
        {
            Task.Delay(1000).Wait();
            return _healthRecordService.GetHealthRecords().Select(r => new HealthRecordPersonDto(r.Pid, r.Name, r.Description, r.CreatedAt));
        }

        [HttpGet]
        [Authorize(AuthenticationSchemes = AuthenticationSchemes.HelseIdBearer, Policy = Policies.EndUserPolicy)]
        public ActionResult<UserDto> GetMe()
        {
            return Ok(new UserDto(
                User.Identity?.Name,
                User.Claims.Select(c => new ClaimDto(c.Type, c.Value)).ToList(),
                User.Identity?.AuthenticationType ?? string.Empty));
        }
    }
}
