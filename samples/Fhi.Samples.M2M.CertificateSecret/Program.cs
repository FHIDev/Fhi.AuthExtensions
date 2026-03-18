using M2M.Host.CertificateSecret;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

public partial class Program
{
    public static async Task Main(string[] args)
    {
        var builder = Host.CreateDefaultBuilder(args);
        builder.ConfigureAppConfiguration((context, config) =>
        {
            var buildConfig = config.Build();
            var apiOption = buildConfig.GetSection("Api").Get<ApiOption>();

            //TODO: when Fhi.Security.Cryptography supports parsing private key from certificate
            /***************************************************************** 
             * For privateKey stored in certificate we need to resolve the certificate at startup to extract 
             * the private key from Windows certificate store.
             * ***************************************************************/
            //config.AddCertificateStorePrivateKey(
            //    "Api:Pem", //Name of the configuration key to store the resolved private JWK
            //    apiOption!.Authentication.CertificateThumbprint,
            //    apiOption.Authentication.CertificateStoreLocation);

        });

        builder.ConfigureServices((context, services) =>
        {
            services.AddHostedService<BackgroundServiceCallingAPI>();
            var apiOption = context.Configuration.GetSection("Api").Get<ApiOption>();
            var jwk = context.Configuration["Api:Pem"] ?? string.Empty;
            services
            .AddClientCredentialsClientOptions(
                        ApiOption.ClientName,
                        apiOption!.Authentication.Authority,
                        apiOption.Authentication.ClientId,
                        PrivateJwk.ParseFromPem(jwk),
                        apiOption.Authentication.Scope)
                    .AddClientCredentialsHttpClient(client =>
                    {
                        client.BaseAddress = new Uri(apiOption.BaseAddress);
                    });
        });

        var app = builder.Build();
        await app.StartAsync();
    }
}
