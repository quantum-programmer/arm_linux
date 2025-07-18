using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;




namespace ARM
{
    internal class CryptoPass
    {
        // Метод для шифрования данных
        public static string Encrypt(string plainText, string key, string iv)
        {
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Encoding.UTF8.GetBytes(key);
                aesAlg.IV = Encoding.UTF8.GetBytes(iv);

                aesAlg.Padding = PaddingMode.PKCS7; // Это стандартный паддинг для AES

                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                    {
                        swEncrypt.Write(plainText);
                    }
                    return Convert.ToBase64String(msEncrypt.ToArray());
                }
            }
        }

        public static string Decrypt(string cipherText, string key, string iv)
        {
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Encoding.UTF8.GetBytes(key);
                aesAlg.IV = Encoding.UTF8.GetBytes(iv);

                aesAlg.Padding = PaddingMode.PKCS7; // Это стандартный паддинг для AES

                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                using (MemoryStream msDecrypt = new MemoryStream(Convert.FromBase64String(cipherText)))
                using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                {
                    return srDecrypt.ReadToEnd();
                }
            }
        }

        public static string GetPass()
        {
            string key = "UHsESZo4DdqGyYJc"; // 16 байт для AES-128
            string iv = "mCy47Cebkubfz6Vn";  // 16 байт для IV
            string encryptedPassword = "rGAkun+rfxyHU9RhNPp3IqL0YYPKjcQ6r0sRuZAR79Y=";
            string decryptedPassword = Decrypt(encryptedPassword, key, iv);


            return decryptedPassword;
        }

    }
}

