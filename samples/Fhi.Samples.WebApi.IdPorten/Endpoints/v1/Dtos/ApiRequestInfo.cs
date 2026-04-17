namespace Fhi.Samples.WebApi.IdPorten.Endpoints.v1.Dtos;

public record ApiRequestInfo(
    string AuthenticationScheme,
    IList<ClaimInfo> UserClaims,
    IDictionary<string, string> RequestHeaders,
    AltinnDecision AltinnAuthorization
);

public record ClaimInfo(string Type, string Value);

public record AltinnDecision(string Decision, string? Message = null);
