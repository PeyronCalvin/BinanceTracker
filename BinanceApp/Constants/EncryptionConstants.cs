using System.Security.Cryptography;
namespace BinanceApp.Models
{
    public class EncryptionConstants
    {
        private static RSA rsaPrivateKey = Encryption.CreateRsaKey(@"Keys/privateKey.pem");
        private static RSA rsaPublicKey = Encryption.CreateRsaKey(@"Keys/publicKey.pem");
        private static string apiKey = File.ReadAllText(@"Keys/apiKey");

        public static RSA RSAPrivateKey => rsaPrivateKey;
        public static RSA RSAPublicKey => rsaPublicKey;
        public static string APIKey => apiKey;
    }
}