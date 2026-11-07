using System.Text.Json;
using System.Text.Json.Serialization;

namespace EndUser.BlazorInteractiveServer.Services
{
    public class ServiceResult<T>
    {
        public required HttpResponseMessage Raw { get; set; }
        public T? Data { get; set; }

        public string? ErrorMessage { get; set; }

        public bool IsError { get; set; } = false;
        public bool ShouldNavigate { get; set; } = false;
        public string? NavigateTo { get; set; } = "/";
    }



    public class BaseService
    {
        protected readonly NavigationService NavigationService;

        public BaseService(NavigationService navigationService)
        {
            NavigationService = navigationService;
        }

        protected async Task<ServiceResult<T>> ExecuteWithErrorHandling<T>(Func<Task<HttpResponseMessage>> request)
        {
            try
            {
                var response = await request();
                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var serialized = JsonSerializer.Deserialize<T?>(
                    content,
                    new JsonSerializerOptions()
                    {
                        PropertyNameCaseInsensitive = true,
                        IncludeFields = true,
                        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                    });

                    return new ServiceResult<T> { Data = serialized, Raw = response };
                }
                else
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        return new ServiceResult<T> { IsError = true, ShouldNavigate = true, NavigateTo = "/", ErrorMessage = $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}", Raw = response };
                    }

                    return new ServiceResult<T> { IsError = true, ErrorMessage = $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}: {content}", Raw = response };
                }
            }
            catch (Exception)
            {
                //Redirect to error
                return default!;
            }
        }
    }

}
