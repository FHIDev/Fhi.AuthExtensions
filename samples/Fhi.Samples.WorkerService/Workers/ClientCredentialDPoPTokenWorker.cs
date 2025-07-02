using Duende.AccessTokenManagement;
using Duende.IdentityModel;
using Duende.IdentityModel.Client;
using Fhi.Authentication.Tokens;
using Microsoft.Extensions.Options;
using WorkerService.Api;

namespace WorkerService.Workers
{
    public class ClientCredentialDPoPTokenWorker : BackgroundService
    {
        private readonly ILogger<ClientCredentialDPoPTokenWorker> _logger;
        private readonly IHttpClientFactory _factory;
        private readonly IDPoPProofService _dPoPProofService;
        private readonly ClientConfiguration _clientConfiguration;
        private readonly IHealthRecordApi _healthRecordApi;

        public ClientCredentialDPoPTokenWorker(
            ILogger<ClientCredentialDPoPTokenWorker> logger,
            IHttpClientFactory factory,
            IOptions<ClientConfiguration> clientConfigurations,
            IDPoPProofService dPoPProofService,
            IHealthRecordApi healthRecordApi)
        {
            _logger = logger;
            _factory = factory;
            _dPoPProofService = dPoPProofService;
            _clientConfiguration = clientConfigurations.Value;
            _healthRecordApi = healthRecordApi;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            /************************************************************************************************
            * Manually getting Dpop token and set authorization header on the API request. 
            * **********************************************************************************************/
            using var client = new HttpClient();
            var discovery = await client.GetDiscoveryDocumentAsync(_clientConfiguration.Authority);

            if (discovery is not null && !discovery.IsError)
            {
                var dpopKey = JwkGenerator.GenerateRsaJwk().PrivateKey;
                var proof = await _dPoPProofService.CreateProofTokenAsync(new DPoPProofRequest
                {
                    Url = discovery.TokenEndpoint!,
                    Method = "POST",
                    DPoPJsonWebKey = dpopKey,
                });
                var nonceRequest = new ClientCredentialsTokenRequest()
                {
                    ClientId = _clientConfiguration.ClientId,
                    Address = discovery.TokenEndpoint,
                    GrantType = OidcConstants.GrantTypes.ClientCredentials,
                    ClientCredentialStyle = ClientCredentialStyle.PostBody,
                    ClientAssertion = new ClientAssertion()
                    {
                        Type = OidcConstants.ClientAssertionTypes.JwtBearer,
                        Value = ClientAssertionTokenHandler.CreateJwtToken(discovery?.Issuer!, _clientConfiguration.ClientId, _clientConfiguration.Secret)
                    },
                    DPoPProofToken = proof?.ProofToken,
                    Scope = _clientConfiguration.Scope
                };

                var response = await client.RequestClientCredentialsTokenAsync(nonceRequest);
                if (response.Error == "use_dpop_nonce" && response.DPoPNonce is not null)
                {
                    var nonceProof = await _dPoPProofService.CreateProofTokenAsync(new DPoPProofRequest
                    {
                        Url = discovery?.TokenEndpoint!,
                        Method = "POST",
                        DPoPJsonWebKey = dpopKey,
                        DPoPNonce = response.DPoPNonce
                    });
                    var tokenRequest = new ClientCredentialsTokenRequest()
                    {
                        ClientId = _clientConfiguration.ClientId,
                        Address = discovery?.TokenEndpoint,
                        GrantType = OidcConstants.GrantTypes.ClientCredentials,
                        ClientCredentialStyle = ClientCredentialStyle.PostBody,
                        ClientAssertion = new ClientAssertion()
                        {
                            Type = OidcConstants.ClientAssertionTypes.JwtBearer,
                            Value = ClientAssertionTokenHandler.CreateJwtToken(discovery?.Issuer!, _clientConfiguration.ClientId, _clientConfiguration.Secret)
                        },
                        DPoPProofToken = nonceProof?.ProofToken,
                        Scope = _clientConfiguration.Scope
                    };

                    response = await client.RequestClientCredentialsTokenAsync(tokenRequest);
                }
                else if (response.IsError)
                {
                    _logger.LogError(response.Error);
                }

                //API request with DPoP token
                var uri = new Uri(new Uri("https://localhost:7150"), "api/v1/integration/health-records");
                var apiProof = await _dPoPProofService.CreateProofTokenAsync(new DPoPProofRequest
                {
                    Url = uri.ToString(),
                    Method = "Get",
                    DPoPJsonWebKey = dpopKey,
                    AccessToken = response.AccessToken
                });
                var apiRequest = new HttpRequestMessage(HttpMethod.Get, uri);
                apiRequest.SetDPoPToken(response.AccessToken!, apiProof!.ProofToken);
                var healthRecordApiResponse = await client.SendAsync(apiRequest);
                _logger.LogInformation("Bearer api response: " + await healthRecordApiResponse.Content.ReadAsStringAsync());
            }

            /************************************************************************************************
            * Sample of using Duende Http Delegation handler to create dpop token 
            * **********************************************************************************************/
            var healthRecordApiDPoPClient = _factory.CreateClient(_clientConfiguration.ClientName + ".dpop");
            var dpopResponse = await healthRecordApiDPoPClient.GetAsync("api/v1/integration/health-records");
            _logger.LogInformation("Dpop weather response: " + await dpopResponse.Content.ReadAsStringAsync());


            /************************************************************************************************
            * Sample of using Refit with Duende Http Delegation handler to access API with DPoP token 
            * **********************************************************************************************/
            var refitHealthRecords = await _healthRecordApi.GetHealthRecordsAsync();
            _logger.LogInformation("Refit health records count: {Count}", refitHealthRecords.Count());

            while (!stoppingToken.IsCancellationRequested)
            {
                if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                }
                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}
