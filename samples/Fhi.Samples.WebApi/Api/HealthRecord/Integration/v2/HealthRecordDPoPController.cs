using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.Services;

namespace Fhi.Samples.WebApi.Api.HealthRecord.Integration.v2
{
    [ApiController]
    [Route("api/v2/integration/health-records")]
    [AllowAnonymous]
    //[Authorize(AuthenticationSchemes = "bearer.integration", Policy = "IntegrationPolicy")]
    public class HealthRecordController(IHealthRecordService healthRecordService) : ControllerBase
    {
        private readonly IHealthRecordService _healthRecordService = healthRecordService;

        [HttpGet]
        public IEnumerable<HealthRecordDto> Get()
        {
            return _healthRecordService.GetHealthRecords().Select(r => new HealthRecordDto()
            {
                AuthorizationHeader = Request.Headers["Authorization"],
                DPoPHeader = Request.Headers["DPoP"]
            });
        }
    }
}
