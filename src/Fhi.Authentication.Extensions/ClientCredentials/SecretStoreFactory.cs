using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Fhi.Authentication.ClientCredentials
{
    /// <summary>
    /// Provides a contract for retrieving private keys in JWK format.
    /// </summary>
    public interface ISecretStore
    {
        /// <summary>
        /// Retrieves the private key as a JSON Web Key (JWK) string.
        /// </summary>
        /// <returns>The private key in JWK format.</returns>
        string GetPrivateKeyAsJwk();
    }

    /// <summary>
    /// Factory interface for creating <see cref="ISecretStore"/> instances.
    /// </summary>
    public interface ISecretStoreFactory
    {
        /// <summary>
        /// Creates an <see cref="ISecretStore"/> instance based on configuration.
        /// </summary>
        /// <param name="clientName">Optional. The name of the client configuration to use.</param>
        /// <returns>An <see cref="ISecretStore"/> instance.</returns>
        ISecretStore CreateSecretStore(string? clientName = null);
    }

    /// <summary>
    /// Default implementation of <see cref="ISecretStoreFactory"/> that creates secret stores
    /// based on the configured secret type (certificate or file).
    /// </summary>
    internal class SecretStoreFactory : ISecretStoreFactory
    {
        private readonly IOptionsMonitor<SecretStoreOptions> _options;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<SecretStoreFactory> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="SecretStoreFactory"/> class.
        /// </summary>
        public SecretStoreFactory(
            IOptionsMonitor<SecretStoreOptions> options,
            IServiceProvider serviceProvider,
            ILogger<SecretStoreFactory> logger)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public ISecretStore CreateSecretStore(string? clientName = null)
        {
            var config = _options.Get(clientName);
            
            if (config == null)
            {
                throw new InvalidOperationException($"Secret store configuration not found for client: {clientName ?? "default"}");
            }

            _logger.LogInformation("Creating secret store of type: {SecretStoreType} for client: {ClientName}", 
                config.SecretStoreType, clientName ?? "default");

            return config.SecretStoreType switch
            {
                SecretStoreType.Certificate => CreateCertificateStore(config),
                SecretStoreType.File => CreateFileStore(config),
                _ => throw new InvalidOperationException($"Unknown secret store type: {config.SecretStoreType}")
            };
        }

        private ISecretStore CreateCertificateStore(SecretStoreOptions config)
        {
            if (string.IsNullOrEmpty(config.CertificateThumbprint))
            {
                throw new InvalidOperationException("Certificate thumbprint is required for certificate secret store");
            }

            if (string.IsNullOrEmpty(config.ClientId))
            {
                throw new InvalidOperationException("ClientId is required for certificate secret store");
            }

            return new CertificateSecretStore(
                config.ClientId,
                config.CertificateThumbprint,
                _serviceProvider.GetRequiredService<ICertificateJwkResolver>(),
                _serviceProvider.GetRequiredService<ILogger<CertificateSecretStore>>()
            );
        }

        private ISecretStore CreateFileStore(SecretStoreOptions config)
        {
            if (string.IsNullOrEmpty(config.PrivateJwk))
            {
                throw new InvalidOperationException("PrivateJwk is required for file secret store");
            }

            return new FileSecretStore(
                config.PrivateJwk,
                _serviceProvider.GetRequiredService<ILogger<FileSecretStore>>()
            );
        }
    }

    /// <summary>
    /// Defines the type of secret store to use.
    /// </summary>
    public enum SecretStoreType
    {
        /// <summary>
        /// Use a file-based secret store (e.g., from environment variable or configuration).
        /// </summary>
        File,
        
        /// <summary>
        /// Use a certificate-based secret store from the Windows certificate store.
        /// </summary>
        Certificate
    }

    /// <summary>
    /// Configuration options for creating secret stores.
    /// </summary>
    public class SecretStoreOptions
    {
        /// <summary>
        /// Gets or sets the type of secret store to create.
        /// </summary>
        public SecretStoreType SecretStoreType { get; set; } = SecretStoreType.File;

        /// <summary>
        /// Gets or sets the client ID (required for certificate stores).
        /// </summary>
        public string? ClientId { get; set; }

        /// <summary>
        /// Gets or sets the certificate thumbprint (required for certificate stores).
        /// </summary>
        public string? CertificateThumbprint { get; set; }

        /// <summary>
        /// Gets or sets the private JWK content (required for file stores).
        /// </summary>
        public string? PrivateJwk { get; set; }
    }
}