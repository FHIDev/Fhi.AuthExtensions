# HttpClient: Client credentials token request(HelseID and EntraID)

HelseID and EntraID enables the client credentials authorization grant [RFC6749 - Client credentials](https://datatracker.ietf.org/doc/html/rfc6749#section-1.3.4)

## Option 1 - Using extension methods

Full sample can be found in [HelseId Sample](https://github.com/FHIDev/Fhi.AuthExtensions/tree/main/samples/Fhi.Samples.M2M.HelseID). The Fhi.Authentication.Extension methods builds on top of [Duende.AccessTokenManagement library](../tutorials/ClientCredentials/Duende-accesstokenmanagement.md).

### Prerequisite: 

- Install package [Fhi.Authentication.Extensions](https://www.nuget.org/packages/Fhi.Authentication.Extensions)
- HelseId or EntraID client


### Step 1: Read and validate settings from configuration

Sample of Appsetting configuration
```
"HelseIdProtectedApi": {
    "BaseAddress": "https://localhost:7150",
    "Authentication": {
      "Authority": "https://helseid-sts.test.nhn.no",
      "Scope": "<Scope for resource>",
      "ClientId": "<HelseID clientId>",
      "PrivateJwk": "<HelseId private jwk>"
    }
  }
```
Reading and validating configuration. See Option definition [here](https://github.com/FHIDev/Fhi.AuthExtensions/blob/main/samples/Fhi.Samples.M2M.HelseID/HelseIdProtectedApiOption.cs)
```
var apiSection = context.Configuration.GetSection("HelseIdProtectedApi");
            services
                    .AddOptions<HelseIdProtectedApiOption>()
                    .Bind(apiSection)
                    .ValidateDataAnnotations()
                    .ValidateOnStart();
```

### Step 2: Register client-credentials named options and connect to HttpClient
This code registers a token client using client-credentials and configures it as a named options instance, where the name corresponds to api.ClientName.
A corresponding named HttpClient is then created with the same name.

This HttpClient is automatically configured with a delegating handler that acquires an access token using the registered client-credentials options.
When the HttpClient sends requests, the delegating handler retrieves a token and attaches it to outgoing requests.

```
 var clientCredentialsOption = services
                //Adding token client options
                .AddClientCredentialsClientOptions(
                    api.ClientName,
                    api.Authentication.Authority,
                    api.Authentication.ClientId,
                    PrivateJwk.ParseFromJson(api.Authentication.PrivateJwk),
                    api.Authentication.Scope)
                    //The DPoP proof key can be enabled by uncommenting the DPoPProofKey.ParseOrDefault(...) line, which configures the token client for DPoP-bound access tokens.
                ////DPoPProofKey.ParseOrDefault(api.Authentication.PrivateJwk))
                //Token client option is bound to the named HttpClient
                .AddClientCredentialsHttpClient(client =>
                {
                    client.BaseAddress = new Uri(api?.BaseAddress!);
                });
                // With Refit
                ////.AddTypedClient(RestService.For<IHealthRecordApi>);
```

### Step 3: Using the named HttpClient in a Service

Inject the API option and HttpClientFactory into the constructor. See [sample service](https://github.com/FHIDev/Fhi.AuthExtensions/blob/main/samples/Fhi.Samples.M2M.HelseID/HealthRecordService.cs)
```
 private readonly HelseIdProtectedApiOption _protectedApiOption;

        public HealthRecordService(
            ILogger<HealthRecordService> logger,
            IHttpClientFactory httpClientFactory,
            IOptions<HelseIdProtectedApiOption> protectedApiOption)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _protectedApiOption = protectedApiOption.Value;
        }
```

Create client with the named httpclient that are bound to the token client
```
//Create client
var client = _httpClientFactory.CreateClient(_protectedApiOption.ClientName);
//API call
var response = await client.GetAsync("api/v1/integration/health-records/helseid-bearer");
```

## Option 2 - Creating your own named options 

See full sampel [here](https://github.com/FHIDev/Fhi.AuthExtensions/blob/main/samples/Fhi.Samples.M2M.MultipleClients/Program.Advanced.cs)

### Step1: Read and validate settings from configuration
```
        var helseIdProtectedApiSection = configuration.GetSection("Apis:HelseIdProtectedApi");
        services
            .AddOptions<HelseIdProtectedApiOption>()
             .Bind(helseIdProtectedApiSection)
             .ValidateDataAnnotations()
             .ValidateOnStart();

```
### Step 2: Register client-credentials named options 
Configure Client credentials options used by the HttpClient to authenticate
```
        var helseIdProtectedApi = helseIdProtectedApiSection.Get<HelseIdProtectedApiOption>() ?? default;

        services
          .AddOptions<ClientAssertionOptions>(helseIdProtectedApi!.ClientName)
          .Configure<IDiscoveryDocumentStore>((options, discoveryStore) =>
          {
              var discoveryDocument = discoveryStore.Get(helseIdProtectedApi!.Authentication.Authority);
              options.Issuer = discoveryDocument?.Issuer ?? string.Empty;
              options.PrivateJwk = helseIdProtectedApi.Authentication.PrivateJwk;
          });

        services
            .AddOptions<ClientCredentialsClient>(helseIdProtectedApi!.ClientName)
            .Configure<IDiscoveryDocumentStore>((options, discoveryStore) =>
            {
                var discoveryDocument = discoveryStore.Get(helseIdProtectedApi!.Authentication.Authority);
                options.TokenEndpoint = discoveryDocument?.TokenEndpoint is not null ? new Uri(discoveryDocument.TokenEndpoint) : null;
                options.ClientId = ClientId.Parse(helseIdProtectedApi.Authentication.ClientId);
                options.Scope = Scope.Parse(helseIdProtectedApi.Authentication.Scope);
            });

```

### Step 3: Connect registered client-credentials options to named HttpClient
 Register HttpClient and connect the token client to be used for authentiation
```
           
        services.AddClientCredentialsHttpClient(helseIdProtectedApi!.ClientName, ClientCredentialsClientName.Parse(helseIdProtectedApi.ClientName), client =>
        {
            client.BaseAddress = new Uri(helseIdProtectedApi?.BaseAddress!);
        });
```
