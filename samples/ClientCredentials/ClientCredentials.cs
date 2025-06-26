using System.Net.Http.Headers;
using System.Text.Json;

namespace Test
{
    public class ClientCredentialsTests
    {
        private readonly HttpClient _client = new();

        /// <summary>
        /// See demo clients in https://demo.duendesoftware.com/
        /// 
        /// 1. Goto https://demo.duendesoftware.com/
        /// 2. Add token endpoint from the discovery document
        /// 3. In Machine-to-Machine find the m2m client and add clientId and clientSecret values
        /// 4. Run API: dotnet run --project .\samples\Fhi.Samples.WebApi\Fhi.Samples.WebApi.csproj and run the test
        /// 
        /// What happens?
        /// </summary>
        [Test]
        public async Task ClientCredential_WithSharedSecret()
        {
            var token = await GetAccessTokenWithSharedSecret(
                "https://demo.duendesoftware.com/connect/token",
                "m2m",
                "secret",
                "resource1.scope1 api");

            var apiUrl = "https://localhost:7150/api/v1/me/health-records";
            var request = new HttpRequestMessage(HttpMethod.Get, apiUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _client.SendAsync(request);
            response.EnsureSuccessStatusCode();
        }

        /// <summary>
        /// 1. Register klientsystem and klientkonfigurasjon in https://selvbetjening.test.nhn.no/
        /// 2. Register API with scope
        /// 3. Update GetAccessTokenWithClientAssertion values
        /// </summary>
        [Test]
        public async Task ClientCredential_WithClientAssertionAsync()
        {
            var token = await GetAccessTokenWithClientAssertion(
                "<helseid token endpoint>",
                "<clientId>",
                "<private_jwk>",
                "<api scope>",
                "<issuer>");

            var apiUrl = "https://localhost:7150/api/v1/integration/weatherforcasts";
            var request = new HttpRequestMessage(HttpMethod.Get, apiUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _client.SendAsync(request);
            response.EnsureSuccessStatusCode();
        }

        /// <summary>
        /// 1. Register klientsystem and klientkonfigurasjon in https://selvbetjening.test.nhn.no/
        /// 2. Register API with scope
        /// 3. Update GetAccessTokenWithClientAssertion values
        /// </summary>
        [Test]
        public async Task ClientCredential_WithDpop()
        {
            using var client = new HttpClient();
            var tokenEndpoint = "https://demo.duendesoftware.com/connect/token";
            var clientId = "m2m.dpop.nonce";
            var scope = "api";
            var clientSecret = "secret";


            /***Request token to get nonce***/
            //var assertion = TokenHandlers.CreateJwtToken(issuer, clientId, jwk);
            var nonceRequest = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint)
            {
                Content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("grant_type", "client_credentials"),
                    new KeyValuePair<string, string>("client_id", clientId),
                    new KeyValuePair<string, string>("client_secret", clientSecret),
                    //new KeyValuePair<string, string>("client_assertion_type", "urn:ietf:params:oauth:client-assertion-type:jwt-bearer"),
                    //new KeyValuePair<string, string>("client_assertion", assertion),
                    new KeyValuePair<string, string>("scope", scope)
                })
            };
            var dpopKey = TokenHandlers.CreateDPoPKey();
            var dpopProof = TokenHandlers.CreateDPoPProof("tokenEndpoint", HttpMethod.Post.ToString(), dpopKey);
            nonceRequest.Headers.Add("DPoP", dpopProof);

            var response = await client.SendAsync(nonceRequest);


            /***Request token with DPoP proof with nonce***/
            var tokenRequest = new HttpRequestMessage(HttpMethod.Post, "<tokenendpoint>")
            {
                Content = new FormUrlEncodedContent(new[]
               {
                    new KeyValuePair<string, string>("grant_type", "client_credentials"),
                    new KeyValuePair<string, string>("client_id", ""),
                     new KeyValuePair<string, string>("client_secret", clientSecret),
                    //new KeyValuePair<string, string>("client_assertion_type", "urn:ietf:params:oauth:client-assertion-type:jwt-bearer"),
                    //new KeyValuePair<string, string>("client_assertion", assertion),
                    new KeyValuePair<string, string>("scope", "")
                })
            };
            var dpopProofWithNonce = TokenHandlers.CreateDPoPProof("", HttpMethod.Post.ToString(), "response nonce");
            nonceRequest.Headers.Add("DPoP", dpopProofWithNonce);

            response = await client.SendAsync(nonceRequest);

            var content = await response.Content.ReadAsStringAsync();
            var json = JsonSerializer.Deserialize<JsonElement>(content);

            /***Request API with DPoP token***/

            var apiUrl = "https://localhost:7150/api/v1/integration/weatherforcasts";
            var apiRequest = new HttpRequestMessage(HttpMethod.Get, apiUrl);
            apiRequest.Headers.Authorization = new AuthenticationHeaderValue("DPOP", json.GetProperty("access_token").GetString());
            apiRequest.Headers.Add("DPOP", json.GetProperty("access_token").GetString());

            response = await _client.SendAsync(nonceRequest);
            response.EnsureSuccessStatusCode();
        }


        private async Task<string> GetAccessTokenWithSharedSecret(string tokenEndpoint, string clientId, string clientSecret, string scope)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint)
            {
                Content = new FormUrlEncodedContent(new[]
                {
                new KeyValuePair<string, string>("grant_type", "client_credentials"),
                new KeyValuePair<string, string>("client_id", clientId),
                new KeyValuePair<string, string>("client_secret", clientSecret),
                new KeyValuePair<string, string>("scope", scope)
            })
            };

            var response = await _client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var json = JsonSerializer.Deserialize<JsonElement>(content);
            return json.GetProperty("access_token").GetString()!;
        }

        private async Task<string> GetAccessTokenWithClientAssertion(string tokenEndpoint, string clientId, string jwk, string scope, string issuer)
        {
            var assertion = TokenHandlers.CreateJwtToken(issuer, clientId, jwk);

            var request = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint)
            {
                Content = new FormUrlEncodedContent(new[]
                {
                new KeyValuePair<string, string>("grant_type", "client_credentials"),
                new KeyValuePair<string, string>("client_id", clientId),
                new KeyValuePair<string, string>("client_assertion_type", "urn:ietf:params:oauth:client-assertion-type:jwt-bearer"),
                new KeyValuePair<string, string>("client_assertion", assertion),
                new KeyValuePair<string, string>("scope", "api.read")
            })
            };

            var response = await _client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var json = JsonSerializer.Deserialize<JsonElement>(content);
            return json.GetProperty("access_token").GetString()!;
        }

        /// <summary>
        /// Not ready
        /// </summary>
        /// <param name="tokenEndpoint"></param>
        /// <param name="clientId"></param>
        /// <param name="jwk"></param>
        /// <param name="scope"></param>
        /// <param name="issuer"></param>
        /// <returns></returns>
        static async Task<string> GetDpopToken(string tokenEndpoint, string clientId, string jwk, string scope, string issuer)
        {
            using var client = new HttpClient();

            var assertion = TokenHandlers.CreateJwtToken(issuer, clientId, jwk);
            //var dpopProof = CreateDpopProof(tokenEndpoint, "POST");

            var request = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint)
            {
                Content = new FormUrlEncodedContent(new[]
                {
                new KeyValuePair<string, string>("grant_type", "client_credentials"),
                new KeyValuePair<string, string>("client_id", clientId),
                new KeyValuePair<string, string>("client_assertion_type", "urn:ietf:params:oauth:client-assertion-type:jwt-bearer"),
               // new KeyValuePair<string, string>("client_assertion", clientAssertion),
                new KeyValuePair<string, string>("scope", "api.read")
            })
            };

            //   request.Headers.Add("DPoP", dpopProof);

            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var json = JsonSerializer.Deserialize<JsonElement>(content);
            return json.GetProperty("access_token").GetString()!;
        }
    }
}