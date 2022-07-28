using System.Text;
using System.Security.Cryptography;

using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Crypto.Paddings;
using Org.BouncyCastle.Crypto.Engines;

namespace msgfiles
{
    public static class Utils
    {
        public static string GenChallenge()
        {
            return GenToken().Substring(0, 6).ToUpper();
        }

        public static string GenToken()
        {
            return Utils.Hash256Str(Guid.NewGuid().ToString() + DateTime.UtcNow.Ticks);
        }

        public static string Hash256Str(string str)
        {
            using (var hasher = SHA256.Create())
                return BytesToHex(hasher.ComputeHash(Encoding.UTF8.GetBytes(str + "some hash is delicious, I must admit")));
        }

        public static string BytesToHex(byte[] bytes)
        {
            var builder = new StringBuilder(bytes.Length * 2);
            for (int b = 0; b < bytes.Length; ++b)
                builder.Append(bytes[b].ToString("x2"));
            return builder.ToString();
        }

        public static byte[] HexToBytes(string hex)
        {
            return
                Enumerable.Range(0, hex.Length / 2)
                .Select(x => Convert.ToByte(hex.Substring(x * 2, 2), 16))
                .ToArray();
        }

        public static void NormalizeDict(Dictionary<string, string> dict, IEnumerable<string> keys)
        {
            foreach (string key in keys)
            {
                if (!dict.ContainsKey(key))
                    dict.Add(key, "");
                else
                    dict[key] = dict[key].Trim();
            }
        }

        public static Exception SmashExp(Exception exp)
        {
            while (exp.InnerException != null)
                exp = exp.InnerException;
            return exp;
        }

        public static string SumExp(Exception exp)
        {
            exp = SmashExp(exp);
            return $"{exp.GetType().FullName}: {exp.Message}";
        }

        public static MemoryStream CombineArrays(byte[] array1, byte[] array2)
        {
            var stream = new MemoryStream(array1.Length + array2.Length);
            stream.Write(array1, 0, array1.Length);
            stream.Write(array2, 0, array2.Length);
            if (stream.Length > 0)
                stream.Seek(0, SeekOrigin.Begin);
            return stream;
        }

        public static string Encrypt(string plainText, string key)
        {
            return 
                BytesToHex
                (
                    AES
                    (
                        forEncryption: true, 
                        key: key, 
                        data: Encoding.UTF8.GetBytes(plainText)
                    )
                );
        }

        public static string Decrypt(string cipherText, string key)
        {
            return 
                Encoding.UTF8.GetString
                (
                    AES
                    (
                        forEncryption: false, 
                        key: key, 
                        data: HexToBytes(cipherText)
                    )
                );
        }

        private static byte[] AES(bool forEncryption, string key, byte[] data)
        {
            byte[] hashed_key;
            using (var hasher = MD5.Create())
                hashed_key = hasher.ComputeHash(Encoding.UTF8.GetBytes(key + "I like good hash"));

            var cipher = new PaddedBufferedBlockCipher(new AesEngine(), new Pkcs7Padding());
            cipher.Init(forEncryption, new KeyParameter(hashed_key));

            return cipher.DoFinal(data);
        }
    }
}
