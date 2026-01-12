using Client.ClientCredentialsWorkers.MultipleHttpClients.Options;
using Duende.AccessTokenManagement;
using Duende.AccessTokenManagement.DPoP;
using Fhi.Authentication;
using Fhi.Samples.WorkerServiceMultipleClients.MultipleClientVariant2;

public partial class Program
{
    public static IHostBuilder CreateHostBuilderUsingExtensions(string[] args) =>
       Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((hostingContext, config) =>
            {
                config.AddJsonFile("appsettings.MultipleHttpClients.json", optional: false, reloadOnChange: true);
            })
           .ConfigureServices((context, services) =>
           {
               IConfiguration configuration = context.Configuration;

               /***********************************************************************************************
                * HttpClient for HelseID protected API
                * *********************************************************************************************/
               var helseIdApi = configuration.GetSection("Apis:HelseIdProtectedApi").Get<HelseIdProtectedApiOption>() ?? new HelseIdProtectedApiOption();
               var helseIdCredentialsOption = services
                   .AddClientCredentialsClientOptions(
                       helseIdApi.ClientName,
                       helseIdApi.Authentication.Authority,
                       helseIdApi.Authentication.ClientId,
                       PrivateJwk.ParseFromJson(helseIdApi.Authentication.PrivateJwk),
                       helseIdApi.Authentication.Scope,
                       DPoPProofKey.ParseOrDefault(helseIdApi.Authentication.PrivateJwk));
               
               // Optional: Configure custom client assertion expiration (default is 10 seconds)
               helseIdCredentialsOption.ClientAssertionOptions!.Configure(options => options.ExpirationSeconds = 30);
               
               helseIdCredentialsOption
                   .AddClientCredentialsHttpClient(client =>
                       {
                           client.BaseAddress = new Uri(helseIdApi?.BaseAddress!);
                       });

               /***********************************************************************************************
                * HttpClient for Duende protected API
                * *********************************************************************************************/
               var duendeApi = configuration.GetSection("Apis:DuendeProtectedApi").Get<DuendeProtectedApiOption>() ?? new DuendeProtectedApiOption();
               var clientCredentialsOption = services
                   .AddClientCredentialsClientOptions(
                       duendeApi.ClientName,
                       duendeApi.Authentication.Authority,
                       duendeApi.Authentication.ClientId,
                       SharedSecret.Parse(duendeApi.Authentication.SharedSecret),
                       helseIdApi.Authentication.Scope)
                   .AddClientCredentialsHttpClient(client =>
                   {
                       client.BaseAddress = new Uri(duendeApi?.BaseAddress!);
                   });

               services.AddDistributedMemoryCache();
               services.AddHostedService<WorkerMultipleHttpClients>();
               //Add token management services. Must be added after
               services.AddClientCredentialsTokenManagement();
               services.AddInMemoryDiscoveryService([helseIdApi.Authentication.Authority, duendeApi.Authentication.Authority]);
           });

}
