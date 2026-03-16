using M2M.Host.CertificateSecret;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

public partial class Program
{
    public static async Task Main(string[] args)
    {
        var builder = Host.CreateDefaultBuilder(args);
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
        });
        builder.ConfigureServices((context, services) =>
        {
            var apiSection = context.Configuration.GetSection("HelseIdProtectedApi");
            var api = apiSection.Get<HelseIdProtectedApiOption>();

            //services
            //.AddClientCredentialsClientOptions(
            //    "clientName",
            //    api.Authentication.Authority,
            //    api.Authentication.ClientId,
            //    api.Authentication.CertificateThumbprint,
            //    api.Authentication.Scope)
            //.AddClientCredentialsHttpClient(client =>
            //{
            //    client.BaseAddress = new Uri(api.BaseAddress);
            //});
        });

        var app = builder.Build();
        await app.StartAsync();
    }

    //private static void ConfigureWithSecretStore(IServiceCollection services, IConfigurationSection apiSection)
    //{
    //    var api = apiSection.Get<HelseIdProtectedApiOption>()!;

    //    // Register the appropriate ISecretStore implementation based on configuration
    //    var certificateThumbprint = apiSection.GetValue<string>("Authentication:Certificate:Thumbprint");

    //    if (!string.IsNullOrEmpty(certificateThumbprint))
    //    {
    //        services.AddCertificateStoreKeyHandler();
    //        services.AddSingleton<CertificateSecretManager>();

    //        services.AddSingleton<ISecretStore>(sp =>
    //        {
    //            var certificateOptions = new CertificateOptions
    //            {
    //                Thumbprint = certificateThumbprint,
    //                StoreLocation = CertificateStoreLocation.CurrentUser
    //            };

    //            return new CertificateSecretStore(
    //                certificateOptions,
    //                sp.GetRequiredService<IPrivateKeyHandler>(),
    //                sp.GetRequiredService<ILogger<CertificateSecretStore>>(),
    //                sp.GetRequiredService<CertificateSecretManager>());
    //        });
    //    }
    //    else if (!string.IsNullOrEmpty(api.Authentication.PrivateJwk))
    //    {
    //        services.AddSingleton<ISecretStore>(sp =>
    //            new FileSecretStore(
    //                api.Authentication.PrivateJwk,
    //                sp.GetRequiredService<ILogger<FileSecretStore>>()));
    //    }

    //    // Configure client credentials using the ISecretStore
    //    services
    //        .AddClientCredentialsClientOptions(
    //            HelseIdProtectedApiOption.ClientName,
    //            api.Authentication.Authority,
    //            api.Authentication.ClientId,
    //            api.Authentication.Scope)
    //        .AddClientCredentialsHttpClient(client =>
    //        {
    //            client.BaseAddress = new Uri(api.BaseAddress);
    //        });

    //    // Add token management services
    //    services.AddDistributedMemoryCache();
    //    services.AddClientCredentialsTokenManagement();
    //    services.AddInMemoryDiscoveryService([api.Authentication.Authority]);
    //}
}
