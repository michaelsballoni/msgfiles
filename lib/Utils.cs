using System.Text;
using System.Security.Cryptography;

namespace msgfiles
{
    public static class Utils
    {
        public static string GenChallenge()
        {
            return GenToken().Substring(6).ToUpper();
        }

        public static string GenToken()
        {
            return Utils.Hash256Str(Guid.NewGuid().ToString() + DateTime.UtcNow.Ticks);
        }

        public static string Hash256Str(string str)
        {
            using (SHA256 hasher = SHA256.Create())
                return BytesToStr(hasher.ComputeHash(Encoding.UTF8.GetBytes(str)));
        }

        public static string BytesToStr(byte[] bytes)
        {
            StringBuilder builder = new StringBuilder(bytes.Length * 2);
            for (int b = 0; b < bytes.Length; ++b)
                builder.Append(bytes[b].ToString("x2"));
            return builder.ToString();
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
    }
}
