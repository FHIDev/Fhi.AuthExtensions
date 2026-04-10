namespace Fhi.Samples.WebApi.DPoPProtected.Endpoints.v1.Dtos;

public record ApiRequestInfo(
    string AuthenticationScheme,
    IList<ClaimInfo> UserClaims,
    IDictionary<string, string> RequestHeaders
);

public record ClaimInfo(string Type, string Value);
