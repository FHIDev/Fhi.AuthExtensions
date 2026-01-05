using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Fhi.Authentication.ClientCredentials;

/// <summary>
/// Secret store implementation that retrieves JWK from configuration or environment variables.
/// </summary>
public class FileSecretStore : ISecretStore
{
    private readonly string _privateJwkJson;
    private readonly ILogger<FileSecretStore> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileSecretStore"/> class.
    /// </summary>
    /// <param name="privateJwkJson">The private JWK in JSON format.</param>
    /// <param name="logger">The logger instance.</param>
    /// <exception cref="ArgumentNullException">Thrown if privateJwkJson or logger is null.</exception>
    public FileSecretStore(string privateJwkJson, ILogger<FileSecretStore> logger)
    {
        if (string.IsNullOrWhiteSpace(privateJwkJson))
            throw new ArgumentException("Private JWK JSON cannot be null or empty.", nameof(privateJwkJson));
        
        _privateJwkJson = privateJwkJson;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public PrivateJwk GetPrivateJwk()
    {
        _logger.LogDebug("FileSecretStore: Retrieving JWK from configuration");
        
        // Key can be stored as environment variable or in user secret 
        string privateKey = Environment.GetEnvironmentVariable("HelseIdPrivateJwk") ?? _privateJwkJson;
        
        _logger.LogDebug("Found private key with length: {KeyLength}", privateKey.Length);
        
        return PrivateJwk.ParseFromJson(privateKey);
    }
}