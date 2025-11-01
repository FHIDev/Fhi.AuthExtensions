using Api.WebApi.Hosting;
using Fhi.Authorization;
using Fhi.Samples.WebApi.Api.HealthRecord.Integration.v1.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.Services;

namespace WebApi.Api.HealthRecord.Integration.v1
{
    [ApiController]
    [Route("api/v1/integration/health-records")]
    public class HealthRecordController(IHealthRecordService healthRecordService) : ControllerBase
    {
        private readonly IHealthRecordService _healthRecordService = healthRecordService;

        [HttpGet("helseid-bearer")]
        [Scope("fhi:authextensions.samples/access")]
        [Authorize(AuthenticationSchemes = AuthenticationSchemes.HelseIdBearer, Policy = Policies.IntegrationPolicy)]
        public IEnumerable<HealthRecordDto> GetWithHelseIdBearerToken()
        {
            return _healthRecordService.GetHealthRecords().Select(r => new HealthRecordDto(r.Name, r.Description, r.CreatedAt));
        }

        [HttpGet("helseid-dpop")]
        [Scope("fhi:authextensions.samples/access")]
        [Authorize(AuthenticationSchemes = AuthenticationSchemes.HelseIdDPoP, Policy = Policies.IntegrationPolicy)]
        public IEnumerable<HealthRecordDto> GetWithHelseIdDPoPToken()
        {
            return _healthRecordService.GetHealthRecords().Select(r => new HealthRecordDto(r.Name, r.Description, r.CreatedAt));
        }

        [HttpGet("duende")]
        [Scope("api")]
        [Authorize(AuthenticationSchemes = AuthenticationSchemes.Duende, Policy = Policies.IntegrationPolicy)]
        public IEnumerable<HealthRecordDto> GetWithDuendeToken()
        {
            return _healthRecordService.GetHealthRecords().Select(r => new HealthRecordDto(r.Name, r.Description, r.CreatedAt));
        }

        [HttpGet("maskinporten")]
        [Scope("fhi:authextensionssample.access")]
        [Authorize(AuthenticationSchemes = AuthenticationSchemes.MaskinPorten, Policy = Policies.IntegrationPolicy)]
        public IEnumerable<HealthRecordDto> GetWithMaskinPortenToken()
        {
            return _healthRecordService.GetHealthRecords().Select(r => new HealthRecordDto(r.Name, r.Description, r.CreatedAt));
        }
    }
}
