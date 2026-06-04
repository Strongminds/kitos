using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Spectre.Console;

public class SAMLDecryptor
{
    public static byte[] DecryptSymmetricKey(string encryptedKeyBase64, string privateKeyPath, string certificatePassword)
    {
        var keyStorageAttempts = new[]
        {
            (X509KeyStorageFlags.EphemeralKeySet,                                "EphemeralKeySet"),
            (X509KeyStorageFlags.DefaultKeySet,                                  "DefaultKeySet"),
            (X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.Exportable, "MachineKeySet+Exportable"),
        };

        var paddingModes = new[]
        {
            (RSAEncryptionPadding.OaepSHA1,   "OAEP SHA-1"),
            (RSAEncryptionPadding.OaepSHA256, "OAEP SHA-256"),
            (RSAEncryptionPadding.Pkcs1,      "PKCS#1 v1.5"),
        };

        byte[] encryptedKeyBytes = Convert.FromBase64String(encryptedKeyBase64);

        foreach (var (storageFlags, _) in keyStorageAttempts)
        {
            X509Certificate2 certificate;
            try
            {
                // X509CertificateLoader is the non-obsolete replacement for the X509Certificate2 constructor (SYSLIB0057)
                certificate = X509CertificateLoader.LoadPkcs12FromFile(privateKeyPath, certificatePassword, storageFlags);
            }
            catch (CryptographicException)
            {
                continue;
            }

            var rsa = certificate.GetRSAPrivateKey();
            if (rsa is null)
                continue;

            int rsaKeySizeBytes = rsa.KeySize / 8;
            if (rsaKeySizeBytes != encryptedKeyBytes.Length)
                AnsiConsole.MarkupLine(
                    $"[yellow]Warning:[/] RSA key is {rsaKeySizeBytes} bytes but encrypted key is {encryptedKeyBytes.Length} bytes — decryption may fail.");

            foreach (var (padding, paddingName) in paddingModes)
            {
                try
                {
                    byte[] symmetricKey = rsa.Decrypt(encryptedKeyBytes, padding);

                    var table = new Table().NoBorder().HideHeaders();
                    table.AddColumn("k").AddColumn("v");
                    table.AddRow("[grey]Subject[/]", Markup.Escape(certificate.Subject));
                    table.AddRow("[grey]Serial[/]",  certificate.SerialNumber);
                    table.AddRow("[grey]Key[/]",     $"{rsa.KeySize} bits · {paddingName}");
                    AnsiConsole.Write(table);
                    AnsiConsole.MarkupLine(
                        $"[green]Symmetric key decrypted[/] [grey]({symmetricKey.Length * 8}-bit)[/]");

                    return symmetricKey;
                }
                catch (CryptographicException)
                {
                    // try next padding
                }
            }
        }

        throw new CryptographicException(
            "Failed to decrypt the symmetric key with any combination of key storage flags and RSA padding mode.");
    }

    public static string DecryptAssertion(string encryptedAssertionBase64, byte[] symmetricKey, string? encryptionAlgorithm = null)
    {
        byte[] encryptedAssertionBytes = Convert.FromBase64String(encryptedAssertionBase64);

        const string gcmUri = "http://www.w3.org/2009/xmlenc11#aes256-gcm";

        bool useGcm = string.Equals(encryptionAlgorithm, gcmUri, StringComparison.OrdinalIgnoreCase)
                      || string.IsNullOrEmpty(encryptionAlgorithm); // auto-detect: try GCM first

        if (useGcm)
        {
            // AES-256-GCM (xmlenc11#aes256-gcm): layout is [nonce:12][ciphertext:n][tag:16]
            const int nonceSize = 12;
            const int tagSize   = 16;

            if (encryptedAssertionBytes.Length >= nonceSize + tagSize)
            {
                try
                {
                    byte[] nonce      = encryptedAssertionBytes[..nonceSize];
                    byte[] cipherText = encryptedAssertionBytes[nonceSize..^tagSize];
                    byte[] tag        = encryptedAssertionBytes[^tagSize..];

                    using var aesGcm = new AesGcm(symmetricKey, tagSizeInBytes: tagSize);
                    byte[] plainText = new byte[cipherText.Length];
                    aesGcm.Decrypt(nonce, cipherText, tag, plainText);
                    return Encoding.UTF8.GetString(plainText);
                }
                catch (CryptographicException) when (string.IsNullOrEmpty(encryptionAlgorithm))
                {
                    // GCM failed during auto-detection — fall through to CBC
                }
            }

            // If algorithm was explicitly GCM but data is too short, give a clear error
            if (!string.IsNullOrEmpty(encryptionAlgorithm))
                throw new CryptographicException(
                    $"Assertion data too short ({encryptedAssertionBytes.Length} bytes) to contain a GCM nonce and tag.");
        }

        {
            // AES-256-CBC (xmlenc#aes256-cbc): layout is [iv:16][ciphertext:n]
            const int ivSize = 16;

            if (encryptedAssertionBytes.Length < ivSize)
                throw new CryptographicException(
                    $"Assertion data too short ({encryptedAssertionBytes.Length} bytes) to contain a CBC IV.");

            byte[] iv         = encryptedAssertionBytes[..ivSize];
            byte[] cipherText = encryptedAssertionBytes[ivSize..];

            using var aes = System.Security.Cryptography.Aes.Create();
            aes.Key     = symmetricKey;
            aes.IV      = iv;
            aes.Mode    = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var decryptor = aes.CreateDecryptor();
            byte[] plainText = decryptor.TransformFinalBlock(cipherText, 0, cipherText.Length);
            return Encoding.UTF8.GetString(plainText);
        }
    }
}