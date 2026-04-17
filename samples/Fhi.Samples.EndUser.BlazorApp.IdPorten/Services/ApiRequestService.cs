namespace BlazorApp.IdPorten.Services;

public record ApiRequestInfo(
    string AuthenticationScheme,
    IList<ClaimInfo> UserClaims,
    IDictionary<string, string> RequestHeaders,
    AltinnDecision AltinnAuthorization
);

public record ClaimInfo(string Type, string Value);

public record AltinnDecision(string Decision, string? Message = null);

public class ApiRequestService : BaseService
{
    private readonly IHttpClientFactory _factory;

    public ApiRequestService(IHttpClientFactory factory, NavigationService navigationService) : base(navigationService)
    {
        _factory = factory;
    }

    public async Task<ServiceResult<ApiRequestInfo>> GetApiRequestInfo()
    {
        return await ExecuteWithErrorHandling<ApiRequestInfo>(async () =>
        {
            var client = _factory.CreateClient("IdPortenApi");
            return await client.GetAsync("/v1/api-request");
        });
    }
}
