using System.Security.Cryptography.X509Certificates;

namespace AirDropWindows.Core.Interfaces;

/// <summary>
/// Service for security operations (certificates, TLS, identity)
/// </summary>
public interface ISecurityService
{
    /// <summary>
    /// Get or create a self-signed certificate for this device
    /// </summary>
    Task<X509Certificate2> GetOrCreateCertificateAsync();

    /// <summary>
    /// Validate a certificate from a remote device
    /// </summary>
    Task<bool> ValidateCertificateAsync(X509Certificate2 certificate);

    /// <summary>
    /// Generate identity record hash from email/phone
    /// </summary>
    string GenerateIdentityRecord(string? email, string? phoneNumber);

    /// <summary>
    /// Encrypt data for transmission
    /// </summary>
    byte[] Encrypt(byte[] data, X509Certificate2 recipientCertificate);

    /// <summary>
    /// Decrypt received data
    /// </summary>
    byte[] Decrypt(byte[] encryptedData);

    /// <summary>
    /// Get the current certificate thumbprint
    /// </summary>
    string? GetCertificateThumbprint();

    /// <summary>
    /// Check if certificate needs renewal
    /// </summary>
    bool NeedsCertificateRenewal();

    /// <summary>
    /// Renew the device certificate
    /// </summary>
    Task<X509Certificate2> RenewCertificateAsync();
}
