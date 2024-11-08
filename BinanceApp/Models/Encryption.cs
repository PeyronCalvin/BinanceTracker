using System.Security.Cryptography;
using System.Text;

namespace BinanceApp.Models
{
    public class Encryption
    {

        public static string Sign(RSA rsaPrivateKey, string payload)
        {
            byte[] requestBytes = Encoding.ASCII.GetBytes(payload);
            string signature = Convert.ToBase64String(rsaPrivateKey.SignData(requestBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));
            string urlSafeSignature = signature
                .Replace("+", "%2B")
                .Replace("/", "%2F")
                .Replace("=", "%3D");
            return urlSafeSignature;

        }


        public static bool VerifySignature(RSA rsaPublicKey, string payload, string base64Signature)
        {
            base64Signature = base64Signature
                    .Replace("%2B", "+")
                    .Replace("%2F", "/")
                    .Replace("%3D", "=");
            byte[] signature = Convert.FromBase64String(base64Signature);
            byte[] payloadBytes = Encoding.ASCII.GetBytes(payload);
            return rsaPublicKey.VerifyData(payloadBytes, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        }


        private static string ReformKey(string key)
        {
            var reformed = key.Replace("-----BEGIN PRIVATE KEY-----", "")
                              .Replace("-----END PRIVATE KEY-----", "")
                              .Replace("-----BEGIN PUBLIC KEY-----", "")
                              .Replace("-----END PUBLIC KEY-----", "")
                              .Replace("\n", "")
                              .Replace("\r", "")
                              .Trim();
            return reformed;
        }

        public static RSA CreateRsaKey(string file)
        {
            try
            {
                string keyString = File.ReadAllText(file);
                string modifiedKey = ReformKey(keyString);
                byte[] keyBytes = Convert.FromBase64String(modifiedKey);
                RSA rsa = RSA.Create();

                if (keyString.Contains("PRIVATE KEY"))
                {
                    rsa.ImportPkcs8PrivateKey(keyBytes, out _);
                }
                else if (keyString.Contains("PUBLIC KEY"))
                {
                    rsa.ImportSubjectPublicKeyInfo(keyBytes, out _);
                }
                else
                {
                    throw new InvalidOperationException("Invalid key format.");
                }

                return rsa;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating RSA key: {ex.Message}");
                throw;
            }
        }

    }
}