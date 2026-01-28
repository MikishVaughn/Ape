using System.Security.Cryptography;
using System.Text;

namespace Ape.Services;

/// <summary>
/// Service for encrypting and decrypting sensitive credentials using AES256 encryption.
/// Uses a master key from environment variable for encryption/decryption operations.
/// </summary>
public class CredentialEncryptionService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<CredentialEncryptionService> _logger;
    private const string MasterKeyEnvironmentVariable = "MASTER_CREDENTIAL_KEY_ILLUSTRATE";
    private const int KeySize = 256; // AES256
    private const int BlockSize = 128;

    public CredentialEncryptionService(
        IConfiguration configuration,
        ILogger<CredentialEncryptionService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Encrypts a credential value using AES256 encryption.
    /// </summary>
    /// <param name="plainText">The plain text credential to encrypt</param>
    /// <returns>Encrypted bytes</returns>
    public byte[] Encrypt(string plainText)
    {
        if (string.IsNullOrWhiteSpace(plainText))
        {
            throw new ArgumentException("Plain text cannot be null or empty", nameof(plainText));
        }

        var masterKey = GetMasterKey();

        using var aes = Aes.Create();
        aes.KeySize = KeySize;
        aes.BlockSize = BlockSize;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        // Derive key from master key
        var key = DeriveKey(masterKey);
        aes.Key = key;

        // Generate random IV
        aes.GenerateIV();
        var iv = aes.IV;

        using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
        var plainBytes = Encoding.UTF8.GetBytes(plainText);

        using var ms = new MemoryStream();

        // Write IV to beginning of stream (needed for decryption)
        ms.Write(iv, 0, iv.Length);

        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
        {
            cs.Write(plainBytes, 0, plainBytes.Length);
            cs.FlushFinalBlock();
        }

        return ms.ToArray();
    }

    /// <summary>
    /// Decrypts an encrypted credential value.
    /// </summary>
    /// <param name="encryptedBytes">The encrypted bytes to decrypt</param>
    /// <returns>Decrypted plain text credential</returns>
    public string Decrypt(byte[] encryptedBytes)
    {
        if (encryptedBytes == null || encryptedBytes.Length == 0)
        {
            throw new ArgumentException("Encrypted bytes cannot be null or empty", nameof(encryptedBytes));
        }

        var masterKey = GetMasterKey();

        using var aes = Aes.Create();
        aes.KeySize = KeySize;
        aes.BlockSize = BlockSize;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        // Derive key from master key
        var key = DeriveKey(masterKey);
        aes.Key = key;

        // Extract IV from beginning of encrypted data
        var ivSize = BlockSize / 8; // Convert bits to bytes
        var iv = new byte[ivSize];
        Array.Copy(encryptedBytes, 0, iv, 0, ivSize);
        aes.IV = iv;

        // Get encrypted data (after IV)
        var encryptedData = new byte[encryptedBytes.Length - ivSize];
        Array.Copy(encryptedBytes, ivSize, encryptedData, 0, encryptedData.Length);

        using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
        using var ms = new MemoryStream(encryptedData);
        using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        using var reader = new StreamReader(cs, Encoding.UTF8);

        return reader.ReadToEnd();
    }

    /// <summary>
    /// Validates that the master credential key is configured.
    /// </summary>
    /// <returns>True if master key is configured, false otherwise</returns>
    public bool IsMasterKeyConfigured()
    {
        try
        {
            var masterKey = Environment.GetEnvironmentVariable(MasterKeyEnvironmentVariable, EnvironmentVariableTarget.User);

            if (string.IsNullOrWhiteSpace(masterKey))
            {
                masterKey = Environment.GetEnvironmentVariable(MasterKeyEnvironmentVariable, EnvironmentVariableTarget.Machine);
            }

            if (string.IsNullOrWhiteSpace(masterKey))
            {
                masterKey = _configuration[MasterKeyEnvironmentVariable];
            }

            return !string.IsNullOrWhiteSpace(masterKey) && masterKey.Length >= 32;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets the master encryption key from environment variables.
    /// Tries User-level, then Machine-level, then Configuration.
    /// </summary>
    private string GetMasterKey()
    {
        // Try user-level environment variable first
        var masterKey = Environment.GetEnvironmentVariable(MasterKeyEnvironmentVariable, EnvironmentVariableTarget.User);

        // Try machine-level environment variable
        if (string.IsNullOrWhiteSpace(masterKey))
        {
            masterKey = Environment.GetEnvironmentVariable(MasterKeyEnvironmentVariable, EnvironmentVariableTarget.Machine);
        }

        // Try configuration (appsettings.json) as last resort
        if (string.IsNullOrWhiteSpace(masterKey))
        {
            masterKey = _configuration[MasterKeyEnvironmentVariable];
        }

        if (string.IsNullOrWhiteSpace(masterKey))
        {
            _logger.LogError("Master credential key not found. Please set the {KeyName} environment variable.", MasterKeyEnvironmentVariable);
            throw new InvalidOperationException($"Master credential key not configured. Please set the {MasterKeyEnvironmentVariable} environment variable.");
        }

        if (masterKey.Length < 32)
        {
            _logger.LogError("Master credential key is too short. Minimum length is 32 characters.");
            throw new InvalidOperationException("Master credential key must be at least 32 characters long.");
        }

        return masterKey;
    }

    /// <summary>
    /// Derives a consistent 256-bit encryption key from the master key using SHA256.
    /// </summary>
    private byte[] DeriveKey(string masterKey)
    {
        using var sha256 = SHA256.Create();
        return sha256.ComputeHash(Encoding.UTF8.GetBytes(masterKey));
    }

    /// <summary>
    /// Generates a new random master key suitable for credential encryption.
    /// This should only be called once during initial setup.
    /// </summary>
    /// <returns>A random 64-character master key</returns>
    public static string GenerateNewMasterKey()
    {
        // Generate 32 random bytes (256 bits)
        var randomBytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);

        // Convert to base64 for easy storage in environment variables
        return Convert.ToBase64String(randomBytes);
    }
}
