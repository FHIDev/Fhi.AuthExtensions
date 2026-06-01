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
        [Authorize(AuthenticationSchemes = AuthenticationSchemes.HelseIdDPoP, Policy = Policies.EndUserPolicy)]
        public ActionResult<UserDto> GetMe()
        {
            return Ok(new UserDto(
                User.Identity?.Name,
                User.Claims.Select(c => new ClaimDto(c.Type, c.Value)).ToList(),
                User.Identity?.AuthenticationType ?? string.Empty));
        }
    }
}
