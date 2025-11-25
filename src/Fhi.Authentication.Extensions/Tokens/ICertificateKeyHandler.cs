using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using Fhi.Authentication.Tokens;
using Microsoft.IdentityModel.Tokens;

namespace Fhi.Authentication.Tokens
{
    /// <summary>
    /// Abstraction for resolving a certificate private key as a JWK.
    /// </summary>
    public interface ICertificateKeyHandler
    {
        /// <summary>
        /// Returns the private key for the certificate identified by <paramref name="certificateThumbprint"/> serialized as a JWK.
        /// </summary>
        /// <param name="certificateThumbprint">Certificate thumbprint (not null or empty).</param>
        /// <returns>Serialized JWK string.</returns>
        public string GetPrivateKeyAsJwk(string certificateThumbprint);
    }
}

 /// <summary>
/// Resolves an X509 certificate from CurrentUser\My and returns its private key as a JWK.
/// </summary>
public class CertificateKeyHandler : ICertificateKeyHandler
{
    /// <inheritdoc/>
    public string GetPrivateKeyAsJwk(string certificateThumbprint)
    {
        if (string.IsNullOrWhiteSpace(certificateThumbprint))
        {
            throw new ArgumentNullException(nameof(certificateThumbprint));
        }

        var normalizedThumbprint = NormalizeThumbprint(certificateThumbprint);

        var certificate = GetCertificate(normalizedThumbprint);

        return ConvertCertificateToJwk(certificate);
    }

    private static X509Certificate2 GetCertificate(string normalizedThumbprint)
    {
        using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
        store.Open(OpenFlags.ReadOnly);

        var matches = store.Certificates.Find(X509FindType.FindByThumbprint, normalizedThumbprint, validOnly: false);
        if (matches.Count == 0)
        {
            throw new InvalidOperationException($"No certificate found for thumbprint: {normalizedThumbprint}. Make sure the certificate is installed in CurrentUser\\My store");
        }

        var certificate = matches[0];
        ValidateCertificate(certificate);
        return certificate;
    }

    private static string NormalizeThumbprint(string thumbprint) =>
        thumbprint.Replace(" ", string.Empty).ToUpperInvariant();

    private static void ValidateCertificate(X509Certificate2 certificate)
    {
        if (certificate is null)
        {
            throw new ArgumentNullException(nameof(certificate));
        }
        
        if (!certificate.HasPrivateKey)
        {
            throw new InvalidOperationException($"Certificate {certificate.Subject} has no private key available");
        }
        
        if (certificate.NotAfter < DateTime.UtcNow)
        {
            throw new InvalidOperationException($"Certificate {certificate.Subject} has expired on {certificate.NotAfter}");
        }
    }

    private static string ConvertCertificateToJwk(X509Certificate2 certificate)
    {
        using var rsa = certificate.GetRSAPrivateKey();
        if (rsa == null) throw new InvalidOperationException($"Unable to get RSA private key from certificate {certificate.Subject}");

        var rsaParameters = rsa.ExportParameters(true);
        var jwk = new RsaSecurityKey(rsaParameters)
        {
            KeyId = certificate.Thumbprint
        };

        var jsonWebKey = JsonWebKeyConverter.ConvertFromRSASecurityKey(jwk);
        return JsonSerializer.Serialize(jsonWebKey);
    }
}