using System.Net.Http.Headers;

namespace System.Net.Http
{
    internal static class HttpClientExtensions
    {
        internal static HttpClient AddDPoPAuthorizationHeader(this HttpClient client, string token)
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("DPoP", token);
            return client;
        }

        internal static HttpClient AddBearerAuthorizationHeader(this HttpClient client, string token)
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        internal static HttpClient AddDPoPHeader(this HttpClient client, string proof)
        {
            client.DefaultRequestHeaders.Add("DPoP", proof);
            return client;
        }
    }
}
