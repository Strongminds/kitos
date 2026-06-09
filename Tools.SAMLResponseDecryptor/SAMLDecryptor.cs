using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

public class SAMLDecryptor
{
    public static byte[] DecryptSymmetricKey(string encryptedKeyBase64, string privateKeyPath, string certificatePassword)
    {
        // X509CertificateLoader is the non-obsolete replacement for the X509Certificate2 constructor (SYSLIB0057)
        var certificate = X509CertificateLoader.LoadPkcs12FromFile(
            privateKeyPath,
            certificatePassword,
            X509KeyStorageFlags.EphemeralKeySet);

        // GetRSAPrivateKey() returns null if the certificate has no RSA private key (CS8602)
        var rsa = certificate.GetRSAPrivateKey()
            ?? throw new CryptographicException("Certificate does not contain an RSA private key.");

        byte[] encryptedKeyBytes = Convert.FromBase64String(encryptedKeyBase64);

            var paddingModes = new[]
            {
                (RSAEncryptionPadding.OaepSHA1,   "OAEP SHA-1 (rsa-oaep-mgf1p + sha1)"),  // matches EncryptionMethod rsa-oaep-mgf1p / DigestMethod sha1
                (RSAEncryptionPadding.OaepSHA256, "OAEP SHA-256"),
                (RSAEncryptionPadding.Pkcs1,       "PKCS#1 v1.5"),
            };

        foreach (var (padding, name) in paddingModes)
        {
            try
            {
                byte[] symmetricKey = rsa.Decrypt(encryptedKeyBytes, padding);
                Console.WriteLine($"[OK] Decrypted with padding: {name}");
                return symmetricKey;
            }
            catch (CryptographicException)
            {
                Console.WriteLine($"[FAIL] Padding {name} did not work.");
            }
        }

        throw new CryptographicException("Failed to decrypt the symmetric key with any known RSA padding mode.");
    }

    public static string DecryptAssertion(string encryptedAssertionBase64, byte[] symmetricKey)
    {
        byte[] encryptedAssertionBytes = Convert.FromBase64String(encryptedAssertionBase64);

        // AES-256-CBC (xmlenc#aes256-cbc): layout is [iv:16][ciphertext:n]
        const int ivSize = 16; // CBC IV: 128 bits

        if (encryptedAssertionBytes.Length < ivSize)
            throw new CryptographicException(
                $"Assertion data too short ({encryptedAssertionBytes.Length} bytes) to contain a CBC IV.");

        byte[] iv = encryptedAssertionBytes[..ivSize];
        byte[] cipherText = encryptedAssertionBytes[ivSize..];

        Console.WriteLine($"[DEBUG] CBC iv={ivSize}B, ciphertext={cipherText.Length}B");

        using var aes = System.Security.Cryptography.Aes.Create();
        aes.Key = symmetricKey;
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var decryptor = aes.CreateDecryptor();
        byte[] plainText = decryptor.TransformFinalBlock(cipherText, 0, cipherText.Length);

        return Encoding.UTF8.GetString(plainText);
    }
}