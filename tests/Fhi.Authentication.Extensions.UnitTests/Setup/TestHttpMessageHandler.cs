namespace Fhi.Authentication.Extensions.UnitTests.Setup
{
    internal class TestHttpMessageHandler(HttpResponseMessage response) : DelegatingHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(response);
        }
    }
}