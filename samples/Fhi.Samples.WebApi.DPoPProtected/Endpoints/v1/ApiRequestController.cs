using Fhi.Samples.WebApi.DPoPProtected.Endpoints.v1.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Fhi.Samples.WebApi.DPoPProtected.Endpoints.v1;

[ApiController]
[Route("v1/api-request")]
//[Authorize(AuthenticationSchemes = "HelseIdDPoP", Policy = "EndUserPolicy")]
public class ApiRequestController : ControllerBase
{
    [HttpGet]
    public ApiRequestInfo Get()
    {
        var claims = User.Claims
            .Select(c => new ClaimInfo(c.Type, c.Value))
            .ToList();

        var headers = Request.Headers
            .Where(h => !string.IsNullOrEmpty(h.Value))
            .ToDictionary(h => h.Key, h => h.Value.ToString(), StringComparer.OrdinalIgnoreCase);

        return new ApiRequestInfo(
            AuthenticationScheme: User.Identity?.AuthenticationType ?? string.Empty,
            UserClaims: claims,
            RequestHeaders: headers
        );
    }
}
