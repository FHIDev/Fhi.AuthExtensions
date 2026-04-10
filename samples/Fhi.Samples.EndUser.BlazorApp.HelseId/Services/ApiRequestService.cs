namespace BlazorApp.HelseId.Services;

public record ApiRequestInfo(
    string AuthenticationScheme,
    IList<ClaimInfo> UserClaims,
    IDictionary<string, string> RequestHeaders
);

public record ClaimInfo(string Type, string Value);

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
            var client = _factory.CreateClient("DPoPApi");
            return await client.GetAsync("/v1/api-request");
        });
    }
}
