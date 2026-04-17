using Fhi.Samples.WebApi.IdPorten.Endpoints.v1.Dtos;
using Fhi.Samples.WebApi.IdPorten.Services;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Fhi.Samples.WebApi.IdPorten.Endpoints.v1;

[ApiController]
[Route("v1/api-request")]
public class ApiRequestController : ControllerBase
{
    private readonly AltinnAuthorizationService _altinnService;

    public ApiRequestController(AltinnAuthorizationService altinnService)
    {
        _altinnService = altinnService;
    }

    [HttpGet]
    public async Task<ApiRequestInfo> Get()
    {
        var claims = User.Claims
            .Select(c => new ClaimInfo(c.Type, c.Value))
            .ToList();

        var headers = Request.Headers
            .Where(h => !string.IsNullOrEmpty(h.Value))
            .ToDictionary(h => h.Key, h => h.Value.ToString(), StringComparer.OrdinalIgnoreCase);

        /************************************************************************************
         * Extract the raw ID-Porten access token from the Authorization header and the
         * "pid" claim (Norwegian personal identification number) from the token.
         * Then call the Altinn Authorization API to get an authorization decision.
         ************************************************************************************/
        var authHeader = Request.Headers.Authorization.ToString();
        var accessToken = authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? authHeader["Bearer ".Length..]
            : string.Empty;

        var pid = User.FindFirstValue("pid") ?? string.Empty;

        var altinnDecision = string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(pid)
            ? new AltinnDecision("NotApplicable", "Missing access token or pid claim.")
            : await _altinnService.CheckAuthorizationAsync(accessToken, pid);

        return new ApiRequestInfo(
            AuthenticationScheme: User.Identity?.AuthenticationType ?? string.Empty,
            UserClaims: claims,
            RequestHeaders: headers,
            AltinnAuthorization: altinnDecision
        );
    }
}
