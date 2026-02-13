using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using AirDropWindows.Core.Interfaces;
using AirDropWindows.Core.Configuration;
using Serilog;

namespace AirDropWindows.Security;

/// <summary>
/// Security service implementation
/// </summary>
public class SecurityService : ISecurityService
{
    private readonly ILogger _logger;
    private readonly CertificateManager _certificateManager;

    public SecurityService(ILogger logger, SecuritySettings settings)
    {
        _logger = logger;
        _certificateManager = new CertificateManager(logger, settings);
    }

    public async Task<X509Certificate2> GetOrCreateCertificateAsync()
    {
        return await _certificateManager.GetOrCreateCertificateAsync();
    }

    public async Task<bool> ValidateCertificateAsync(X509Certificate2 certificate)
    {
        await Task.CompletedTask;
        // Simplified validation - accept self-signed certificates
        _logger.Debug("Validating certificate: {Thumbprint}", certificate.Thumbprint);
        return certificate.NotAfter > DateTime.UtcNow;
    }

    public string GenerateIdentityRecord(string? email, string? phoneNumber)
    {
        var data = $"{email ?? ""}{phoneNumber ?? ""}";
        var bytes = Encoding.UTF8.GetBytes(data);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }

    public byte[] Encrypt(byte[] data, X509Certificate2 recipientCertificate)
    {
        using var rsa = recipientCertificate.GetRSAPublicKey();
        if (rsa == null)
        {
            throw new InvalidOperationException("No RSA public key available");
        }
        return rsa.Encrypt(data, RSAEncryptionPadding.OaepSHA256);
    }

    public byte[] Decrypt(byte[] encryptedData)
    {
        var cert = _certificateManager.GetOrCreateCertificateAsync().GetAwaiter().GetResult();
        using var rsa = cert.GetRSAPrivateKey();
        if (rsa == null)
        {
            throw new InvalidOperationException("No RSA private key available");
        }
        return rsa.Decrypt(encryptedData, RSAEncryptionPadding.OaepSHA256);
    }

    public string? GetCertificateThumbprint()
    {
        return _certificateManager.GetCertificateThumbprint();
    }

    public bool NeedsCertificateRenewal()
    {
        var cert = _certificateManager.GetOrCreateCertificateAsync().GetAwaiter().GetResult();
        var daysUntilExpiry = (cert.NotAfter - DateTime.UtcNow).TotalDays;
        return daysUntilExpiry < Constants.Certificate.RenewalThresholdDays;
    }

    public async Task<X509Certificate2> RenewCertificateAsync()
    {
        _logger.Information("Renewing certificate");
        return await _certificateManager.GetOrCreateCertificateAsync();
    }
}
