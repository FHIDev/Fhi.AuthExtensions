using Microsoft.Extensions.Logging;

namespace Fhi.Authentication.ClientCredentials;

internal class FileSecretStore(
    string privateJwk,
    ILogger<FileSecretStore> logger) : ISecretStore
{
    public ILogger<FileSecretStore> Logger { get; } = logger;
    
    public string GetPrivateKeyAsJwk()
    {
        Logger.LogInformation("FileSecretStore: Retrieving local secret");
        // Key can be stored as environment variable or in user secret 
        string privateKey = Environment.GetEnvironmentVariable("HelseIdPrivateJwk") ??
                            privateJwk;
        Logger.LogInformation("Found private key with length: {KeyLength}", privateKey.Length);
        return privateKey;
    }
}