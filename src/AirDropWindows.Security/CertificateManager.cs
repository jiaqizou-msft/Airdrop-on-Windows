using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Operators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;
using AirDropWindows.Core.Configuration;
using Serilog;
using X509Certificate = Org.BouncyCastle.X509.X509Certificate;

namespace AirDropWindows.Security;

/// <summary>
/// Manages self-signed certificates for AirDrop authentication
/// </summary>
public class CertificateManager
{
    private readonly ILogger _logger;
    private readonly SecuritySettings _settings;
    private X509Certificate2? _certificate;

    public CertificateManager(ILogger logger, SecuritySettings settings)
    {
        _logger = logger;
        _settings = settings;
    }

    /// <summary>
    /// Get or create certificate
    /// </summary>
    public async Task<X509Certificate2> GetOrCreateCertificateAsync()
    {
        if (_certificate != null && !NeedsCertificateRenewal(_certificate))
        {
            return _certificate;
        }

        _logger.Information("Loading or creating certificate");

        // Try to load from store
        _certificate = LoadCertificateFromStore();

        if (_certificate == null || NeedsCertificateRenewal(_certificate))
        {
            _logger.Information("Generating new self-signed certificate");
            _certificate = await GenerateSelfSignedCertificateAsync();
            StoreCertificate(_certificate);
        }

        return _certificate;
    }

    /// <summary>
    /// Generate self-signed certificate
    /// </summary>
    private async Task<X509Certificate2> GenerateSelfSignedCertificateAsync()
    {
        await Task.CompletedTask;

        var random = new SecureRandom();
        var keyPairGenerator = new RsaKeyPairGenerator();
        keyPairGenerator.Init(new KeyGenerationParameters(random, Constants.Certificate.KeySize));

        var keyPair = keyPairGenerator.GenerateKeyPair();

        var subjectDN = new X509Name($"CN=AirDrop-{Environment.MachineName}");
        var issuerDN = subjectDN;

        var certificateGenerator = new X509V3CertificateGenerator();
        certificateGenerator.SetSerialNumber(BigInteger.ProbablePrime(120, random));
        certificateGenerator.SetSubjectDN(subjectDN);
        certificateGenerator.SetIssuerDN(issuerDN);
        certificateGenerator.SetNotBefore(DateTime.UtcNow.Date);
        certificateGenerator.SetNotAfter(DateTime.UtcNow.Date.AddDays(_settings.CertificateValidityDays));
        certificateGenerator.SetPublicKey(keyPair.Public);

        var signatureFactory = new Asn1SignatureFactory("SHA256WITHRSA", keyPair.Private, random);
        var bouncyCert = certificateGenerator.Generate(signatureFactory);

        // Convert to X509Certificate2
        var cert = X509CertificateLoader.LoadCertificate(bouncyCert.GetEncoded());

        // Add private key
        var rsa = DotNetUtilities.ToRSA((RsaPrivateCrtKeyParameters)keyPair.Private);
        var certWithKey = cert.CopyWithPrivateKey(rsa);

        _logger.Information("Generated certificate: {Thumbprint}, Valid until: {Expiry}",
            certWithKey.Thumbprint, certWithKey.NotAfter);

        return certWithKey;
    }

    /// <summary>
    /// Load certificate from Windows Certificate Store
    /// </summary>
    private X509Certificate2? LoadCertificateFromStore()
    {
        try
        {
            using var store = new X509Store(_settings.CertificateStoreName, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadOnly);

            var certs = store.Certificates
                .Find(X509FindType.FindBySubjectName, $"AirDrop-{Environment.MachineName}", false);

            var cert = certs.Cast<X509Certificate2>()
                .OrderByDescending(c => c.NotAfter)
                .FirstOrDefault();

            if (cert != null)
            {
                _logger.Information("Loaded certificate from store: {Thumbprint}", cert.Thumbprint);
            }

            return cert;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to load certificate from store");
            return null;
        }
    }

    /// <summary>
    /// Store certificate in Windows Certificate Store
    /// </summary>
    private void StoreCertificate(X509Certificate2 certificate)
    {
        try
        {
            using var store = new X509Store(_settings.CertificateStoreName, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadWrite);
            store.Add(certificate);
            _logger.Information("Certificate stored in Windows Certificate Store");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to store certificate");
        }
    }

    /// <summary>
    /// Check if certificate needs renewal
    /// </summary>
    private bool NeedsCertificateRenewal(X509Certificate2 certificate)
    {
        var daysUntilExpiry = (certificate.NotAfter - DateTime.UtcNow).TotalDays;
        return daysUntilExpiry < Constants.Certificate.RenewalThresholdDays;
    }

    public string? GetCertificateThumbprint()
    {
        return _certificate?.Thumbprint;
    }
}
