using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Fhi.Auth.IntegrationTests.Setup
{
    public class TestCertificateBuilder
    {
        private string _subjectName = "CN=Test";
        private int _keySize = 2048;
        private DateTimeOffset _notBefore = DateTimeOffset.Now.AddDays(-1);
        private DateTimeOffset _notAfter = DateTimeOffset.Now.AddYears(1);
        private bool _exportPrivateKey = true;

        public TestCertificateBuilder WithSubject(string subjectName)
        {
            _subjectName = subjectName;
            return this;
        }

        public TestCertificateBuilder WithKeySize(int keySize)
        {
            _keySize = keySize;
            return this;
        }

        public TestCertificateBuilder Expired()
        {
            _notBefore = DateTimeOffset.Now.AddYears(-2);
            _notAfter = DateTimeOffset.Now.AddYears(-1);
            return this;
        }

        public TestCertificateBuilder PublicOnly()
        {
            _exportPrivateKey = false;
            return this;
        }

        /// <summary>
        /// Build and return an X509Certificate2 instance. For private-key certs this returns
        /// a certificate that can be added to the certificate store with store.Add() and the
        /// private key will persist (uses PersistKeySet flag).
        /// </summary>
        public X509Certificate2 Build()
        {
            using var rsa = RSA.Create(_keySize);
            var req = new CertificateRequest(_subjectName, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            using var cert = req.CreateSelfSigned(_notBefore, _notAfter);
            if (_exportPrivateKey)
            {
                using var certWithKey = cert.HasPrivateKey ? cert : cert.CopyWithPrivateKey(rsa);
                // Export to PFX and re-import with PersistKeySet so the key persists when added to store
                var pfx = certWithKey.Export(X509ContentType.Pfx);
#if NET9_0_OR_GREATER
                var persistable = X509CertificateLoader.LoadPkcs12(pfx, null, X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.Exportable);
#else
                var persistable = new X509Certificate2(pfx, (string?)null, X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.Exportable);
#endif
                try {return persistable;} finally{ persistable.Dispose(); }
            }

            var der = cert.Export(X509ContentType.Cert);
            var publicOnly = X509Certificate2.CreateFromPem("-----BEGIN CERTIFICATE-----\n" + Convert.ToBase64String(der, Base64FormattingOptions.InsertLineBreaks) + "\n-----END CERTIFICATE-----\n");

            try
            { return publicOnly; } finally { publicOnly.Dispose(); }
        }

        /// <summary>
        /// Export a PFX (PKCS#12) containing private key using the given password.
        /// Tests that need to import into the OS store should call this and import the bytes.
        /// </summary>
        public byte[] ExportPfx(string password)
        {
            if (!_exportPrivateKey) throw new InvalidOperationException("Cannot export PFX for public-only certificate");
            
            using var rsa = RSA.Create(_keySize);
            var req = new CertificateRequest(_subjectName, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            var cert = req.CreateSelfSigned(_notBefore, _notAfter);
            var certWithKey = cert.HasPrivateKey ? cert : cert.CopyWithPrivateKey(rsa);
            return certWithKey.Export(X509ContentType.Pfx, password);
        }

        public static implicit operator X509Certificate2(TestCertificateBuilder builder) => builder.Build();
    }
}