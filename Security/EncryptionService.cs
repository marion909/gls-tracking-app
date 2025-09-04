using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace GlsTrackingApp.Security
{
    public static class EncryptionService
    {
        private const int KeySize = 256;
        private const int IvSize = 128;
        private const int SaltSize = 32;
        private const int Iterations = 10000;

        public static string Encrypt(string plainText, string password)
        {
            if (string.IsNullOrEmpty(plainText) || string.IsNullOrEmpty(password))
                return string.Empty;

            try
            {
                byte[] salt = new byte[SaltSize];
                using (var rng = RandomNumberGenerator.Create())
                {
                    rng.GetBytes(salt);
                }

                using (var aes = Aes.Create())
                {
                    aes.KeySize = KeySize;
                    aes.BlockSize = IvSize;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;

                    // Derive key from password
                    using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256))
                    {
                        aes.Key = pbkdf2.GetBytes(KeySize / 8);
                        aes.IV = pbkdf2.GetBytes(IvSize / 8);
                    }

                    using (var encryptor = aes.CreateEncryptor())
                    using (var msEncrypt = new MemoryStream())
                    {
                        // Write salt to the beginning
                        msEncrypt.Write(salt, 0, salt.Length);
                        
                        using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                        using (var swEncrypt = new StreamWriter(csEncrypt))
                        {
                            swEncrypt.Write(plainText);
                        }
                        
                        return Convert.ToBase64String(msEncrypt.ToArray());
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Verschlüsselung fehlgeschlagen: {ex.Message}", ex);
            }
        }

        public static string Decrypt(string cipherText, string password)
        {
            if (string.IsNullOrEmpty(cipherText) || string.IsNullOrEmpty(password))
                return string.Empty;

            try
            {
                byte[] fullCipher = Convert.FromBase64String(cipherText);
                
                // Extract salt
                byte[] salt = new byte[SaltSize];
                Array.Copy(fullCipher, 0, salt, 0, SaltSize);
                
                // Extract cipher data
                byte[] cipher = new byte[fullCipher.Length - SaltSize];
                Array.Copy(fullCipher, SaltSize, cipher, 0, cipher.Length);

                using (var aes = Aes.Create())
                {
                    aes.KeySize = KeySize;
                    aes.BlockSize = IvSize;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;

                    // Derive key from password
                    using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256))
                    {
                        aes.Key = pbkdf2.GetBytes(KeySize / 8);
                        aes.IV = pbkdf2.GetBytes(IvSize / 8);
                    }

                    using (var decryptor = aes.CreateDecryptor())
                    using (var msDecrypt = new MemoryStream(cipher))
                    using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    using (var srDecrypt = new StreamReader(csDecrypt))
                    {
                        return srDecrypt.ReadToEnd();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Entschlüsselung fehlgeschlagen: {ex.Message}", ex);
            }
        }

        public static string HashPassword(string password)
        {
            if (string.IsNullOrEmpty(password))
                return string.Empty;

            try
            {
                byte[] salt = new byte[SaltSize];
                using (var rng = RandomNumberGenerator.Create())
                {
                    rng.GetBytes(salt);
                }

                using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256))
                {
                    byte[] hash = pbkdf2.GetBytes(KeySize / 8);
                    
                    // Combine salt and hash
                    byte[] hashBytes = new byte[SaltSize + hash.Length];
                    Array.Copy(salt, 0, hashBytes, 0, SaltSize);
                    Array.Copy(hash, 0, hashBytes, SaltSize, hash.Length);
                    
                    return Convert.ToBase64String(hashBytes);
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Passwort-Hashing fehlgeschlagen: {ex.Message}", ex);
            }
        }

        public static bool VerifyPassword(string password, string hashedPassword)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(hashedPassword))
                return false;

            try
            {
                byte[] hashBytes = Convert.FromBase64String(hashedPassword);
                
                // Extract salt
                byte[] salt = new byte[SaltSize];
                Array.Copy(hashBytes, 0, salt, 0, SaltSize);
                
                // Extract stored hash
                byte[] storedHash = new byte[hashBytes.Length - SaltSize];
                Array.Copy(hashBytes, SaltSize, storedHash, 0, storedHash.Length);

                // Hash the provided password with the stored salt
                using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256))
                {
                    byte[] testHash = pbkdf2.GetBytes(KeySize / 8);
                    
                    // Compare hashes
                    if (testHash.Length != storedHash.Length)
                        return false;
                        
                    for (int i = 0; i < testHash.Length; i++)
                    {
                        if (testHash[i] != storedHash[i])
                            return false;
                    }
                    
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}
