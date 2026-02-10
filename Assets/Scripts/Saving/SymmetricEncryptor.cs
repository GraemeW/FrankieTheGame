using System;
using System.Security.Cryptography;
using System.Text;

namespace Frankie.Saving
{
    public static class SymmetricEncryptor
    {
        private const string _code = "Doyoueverfeellikeaplasticbag";

        public static string EncryptString(string toEncrypt)
        {
            byte[] key = GetKey(_code);

            using Aes aes = Aes.Create();
            using ICryptoTransform encryptor = aes.CreateEncryptor(key, key);

            byte[] plainText = Encoding.UTF8.GetBytes(toEncrypt);
            byte[] encryptedText = encryptor.TransformFinalBlock(plainText, 0, plainText.Length);

            return Convert.ToBase64String(encryptedText);
        }

        public static string DecryptToString(string encodedString)
        {
            byte[] key = GetKey(_code);

            byte[] encryptedData = Convert.FromBase64String(encodedString);
            using Aes aes = Aes.Create();
            using ICryptoTransform encryptor = aes.CreateDecryptor(key, key);
            byte[] decryptedBytes = encryptor.TransformFinalBlock(encryptedData, 0, encryptedData.Length);

            return Encoding.UTF8.GetString(decryptedBytes);
        }

        private static byte[] GetKey(string code)
        {
            byte[] keyBytes = Encoding.UTF8.GetBytes(code);
            using MD5 md5 = MD5.Create();
            return md5.ComputeHash(keyBytes);
        }
    }
}
