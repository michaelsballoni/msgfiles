using System.Text;
using System.IO.Compression;
using System.Security.Cryptography;

using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Paddings;
using Org.BouncyCastle.Crypto.Engines;

using Newtonsoft.Json;

namespace msgfiles
{
    public static class Utils
    {
        /// <summary>
        /// Generate a short challenge string to send to the user
        /// to validate that they have control of their the email address
        /// </summary>
        public static string GenChallenge()
        {
            return HashString(Guid.NewGuid().ToString()).Substring(0, 6).ToUpper();
        }

        /// <summary>
        /// Create a unique token used for sessions and messages
        /// </summary>
        /// <returns></returns>
        public static string GenToken()
        {
            var token = Encoding.UTF8.GetBytes(Guid.NewGuid().ToString());
            return Convert.ToBase64String(token).Trim('=');
        }

        /// <summary>
        /// Compute a salted SHA-256 hash of a string
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string HashString(string str)
        {
            using (var hasher = SHA256.Create())
                return BytesToHex(hasher.ComputeHash(Encoding.UTF8.GetBytes(str + "some hash is delicious, I must admit")));
        }

        /// <summary>
        /// Convert bytes into a hex string
        /// </summary>
        public static string BytesToHex(byte[] bytes)
        {
            var builder = new StringBuilder(bytes.Length * 2);
            for (int b = 0; b < bytes.Length; ++b)
                builder.Append(bytes[b].ToString("x2"));
            return builder.ToString();
        }

        /// <summary>
        /// Convert hex into bytes...in the least efficient way possible
        /// </summary>
        public static byte[] HexToBytes(string hex)
        {
            return
                Enumerable.Range(0, hex.Length / 2)
                .Select(x => Convert.ToByte(hex.Substring(x * 2, 2), 16))
                .ToArray();
        }

        /// <summary>
        /// Ensure that a dictionary has non-null values for a given list of keys
        /// This prevents calling code from having to deal with whether a key
        /// is in the dictionary, or if the value in the dictionary is null
        /// </summary>
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

        /// <summary>
        /// Turn JSON into a string-to-string dictionary
        /// </summary>
        public static Dictionary<string, string> GetMetadata(string json)
        {
            return JsonConvert.DeserializeObject<Dictionary<string, string>>(json) ?? new Dictionary<string, string>();
        }

        /// <summary>
        /// Get to the bottom of things, e.g., without async/await stack noise
        /// </summary>
        public static Exception SmashExp(Exception exp)
        {
            while (exp.InnerException != null)
                exp = exp.InnerException;
            return exp;
        }

        /// <summary>
        /// Yield an exception's type and message, after smashing it
        /// </summary>
        public static string SumExp(Exception exp)
        {
            exp = SmashExp(exp);
            return $"{exp.GetType().FullName}: {exp.Message}";
        }

        /// <summary>
        /// Get a MemoryStream from two byte arrays combined
        /// </summary>
        public static MemoryStream CombineArrays(byte[] array1, byte[] array2)
        {
            var stream = new MemoryStream(array1.Length + array2.Length);
            stream.Write(array1, 0, array1.Length);
            stream.Write(array2, 0, array2.Length);
            if (stream.Length > 0)
                stream.Seek(0, SeekOrigin.Begin);
            return stream;
        }

        /// <summary>
        /// Normalize an email address, and return "" if it's not valid.
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        public static string GetValidEmail(string email)
        {
            email = email.Trim();
            if (email.Length == 0 || email.EndsWith('.'))
                return "";

            try
            {
                if ((new System.Net.Mail.MailAddress(email)).Address != email)
                    return "";
            }
            catch
            {
                return "";
            }

            return email;
        }

        /// <summary>
        /// Parse an email into the email address and the display name
        /// Returns key as email, value as display name
        /// </summary>
        public static KeyValuePair<string, string> ParseEmail(string email)
        {
            int angle = email.LastIndexOf('<');
            if (angle >= 0)
            {
                if (email.IndexOf('<') != angle)
                    throw new InputException($"Invalid email address: {email}");

                string to_name = email.Substring(0, angle).Trim();
                string to_addr = email.Substring(angle).Trim().Trim('<', '>').Trim();

                if (GetValidEmail(to_addr).Length == 0)
                    throw new InputException($"Invalid email address: {to_addr}");
                else
                    return new KeyValuePair<string, string>(to_addr, to_name);
            }
            else
            {
                if (GetValidEmail(email).Length == 0)
                    throw new InputException($"Invalid email address: {email}");
                else
                    return new KeyValuePair<string, string>(email.Trim(), "");
            }
        }

        /// <summary>
        /// Emails are stored and looked up in the messages store in this normalized way
        /// </summary>
        public static string PrepEmailForLookup(string email)
        {
            return ParseEmail(email).Key.ToLower();
        }

        /// <summary>
        /// Basic byte array compression
        /// </summary>
        public static byte[] Compress(ReadOnlySpan<byte> data)
        {
            using (var memStream = new MemoryStream(data.Length / 2)) // hopeful
            {
                Compress(data, memStream);
                return memStream.ToArray();
            }
        }

        /// <summary>
        /// Byte array compression to an output stream
        /// </summary>
        public static void Compress(ReadOnlySpan<byte> data, Stream output)
        {
            using (var zipStream = new GZipStream(output, CompressionLevel.Fastest, leaveOpen: true))
                zipStream.Write(data);
            if (output.Length > 0)
                output.Seek(0, SeekOrigin.Begin);
        }

        /// <summary>
        /// Decompress a buffer into an output buffer
        /// </summary>
        public static byte[] Decompress(byte[] data, int length = -1)
        {
            using (var inputStream = length < 0 ? new MemoryStream(data) : new MemoryStream(data, 0, length))
            using (var outputStream = new MemoryStream())
            using (var zipStream = new GZipStream(inputStream, CompressionMode.Decompress))
            {
                zipStream.CopyTo(outputStream);
                return outputStream.ToArray();
            }
        }

        /// <summary>
        /// Hash the contents of a stream
        /// </summary>
        public static string HashStream(Stream stream)
        {
            using (var hasher = SHA256.Create())
                return BytesToHex(hasher.ComputeHash(stream));
        }

        /// <summary>
        /// Hash the contents of a stream
        /// </summary>
        public static async Task<string> HashStreamAsync(Stream stream)
        {
            using (var hasher = SHA256.Create())
                return BytesToHex(await hasher.ComputeHashAsync(stream).ConfigureAwait(false));
        }

        /// <summary>
        /// Given a number of bytes, return a friendly description
        /// </summary>
        public static string ByteCountToStr(long byteCount)
        {
            long size = byteCount;
            if (size < 1000)
                return $"{size} bytes";
            else if (size < 1000 * 1024)
                return $"{Math.Round((double)size / 1024, 1)} KB";
            else if (size < 1000 * 1024 * 1024)
                return $"{Math.Round((double)size / 1024 / 1024, 1)} MB";
            else
                return $"{Math.Round((double)size / 1024 / 1024 / 1024, 1)} GB";
        }

        /// <summary>
        /// Create a ZIP files from files and folders to include
        /// </summary>
        /// <param name="app">App to use for pacification</param>
        /// <param name="zipFilePath">Path to ZIP file to write to</param>
        /// <param name="paths">Paths to files and folders to include</param>
        public static void CreateZip(IClientApp app, string zipFilePath, IEnumerable<string> paths)
        {
            using (var zip = new Ionic.Zip.ZipFile(zipFilePath))
            {
                zip.CompressionLevel = Ionic.Zlib.CompressionLevel.BestSpeed;
                string lastZipCurrentFilename = "";
                zip.SaveProgress +=
                    (object? sender, Ionic.Zip.SaveProgressEventArgs e) =>
                    {
                        if (e.CurrentEntry != null && e.CurrentEntry.FileName != lastZipCurrentFilename)
                        {
                            lastZipCurrentFilename = e.CurrentEntry.FileName;
                            app.Log(lastZipCurrentFilename);
                        }

                        if (e.TotalBytesToTransfer > 0)
                            app.Progress((double)e.BytesTransferred / e.TotalBytesToTransfer);
                    };
                foreach (var path in paths)
                {
                    if (File.Exists(path))
                        zip.AddFile(path, "");
                    else if (Directory.Exists(path))
                        zip.AddDirectory(path, Path.GetFileName(path));
                    else
                        throw new InputException($"Item to send not found: {path}");
                }

                zip.Save();
            }
        }

        /// <summary>
        /// Summarize the contents of a ZIP file for the benefit of having an idea
        /// whether they are what expected, and safe
        /// </summary>
        public static string ManifestZip(string zipFilePath)
        {
            int file_count = 0;
            long total_byte_count = 0;

            
            StringBuilder entry_lines = new StringBuilder();
            
            Dictionary<string, int> ext_counts = new Dictionary<string, int>();
            
            using (var zip_file = new Ionic.Zip.ZipFile(zipFilePath))
            {
                foreach (var zip_entry in zip_file.Entries)
                {
                    if (zip_entry.IsDirectory)
                        continue;

                    string size_str = 
                        Utils.ByteCountToStr(zip_entry.UncompressedSize);
                    entry_lines.AppendLine($"{zip_entry.FileName} ({size_str})");

                    string ext = Path.GetExtension(zip_entry.FileName).ToUpper();
                    if (ext_counts.ContainsKey(ext))
                        ++ext_counts[ext];
                    else
                        ext_counts[ext] = 1;

                    ++file_count;
                    total_byte_count += zip_entry.UncompressedSize;
                }
            }

            string ext_summary =
                "File Types:\r\n" +
                string.Join
                (
                    "\r\n",
                    ext_counts
                        .Select(kvp => $"{kvp.Key.Trim('.')}: {kvp.Value}")
                        .OrderBy(str => str)
                );

            return
                $"Files: {file_count}" +
                $" - " +
                $"Total: {Utils.ByteCountToStr(total_byte_count)}" +
                $"\r\n\r\n" +
                $"{ext_summary}" +
                $"\r\n\r\n" +
                $"{entry_lines}";
        }

        /// <summary>
        /// Extract a ZIP file's contents into an output directory
        /// </summary>
        /// <param name="app">App used for pacification</param>
        /// <param name="zipFilePath">ZIP file to extract from</param>
        /// <param name="extractionDirPath">Folder to extract to</param>
        public static void ExtractZip(IClientApp app, string zipFilePath, string extractionDirPath)
        {
            using (var zip = new Ionic.Zip.ZipFile(zipFilePath))
            {
                string lastZipCurrentFilename = "";
                zip.ExtractProgress +=
                    (object? sender, Ionic.Zip.ExtractProgressEventArgs e) =>
                    {
                        if (e.CurrentEntry != null && e.CurrentEntry.FileName != lastZipCurrentFilename)
                        {
                            lastZipCurrentFilename = e.CurrentEntry.FileName;
                            app.Log(lastZipCurrentFilename);
                        }

                        if (e.TotalBytesToTransfer > 0)
                            app.Progress((double)e.BytesTransferred / e.TotalBytesToTransfer);
                    };
                zip.ExtractAll(extractionDirPath);
            }
        }

        /// <summary>
        /// Do basic plain+key -> cipher encryption
        /// </summary>
        public static string Encrypt(string plain, string key)
        {
            return 
                BytesToHex
                (
                    AES
                    (
                        forEncryption: true, 
                        key: key, 
                        data: Encoding.UTF8.GetBytes(plain)
                    )
                );
        }

        /// <summary>
        /// Do basic cipher+key -> plain decryption
        /// </summary>
        public static string Decrypt(string cipher, string key)
        {
            return 
                Encoding.UTF8.GetString
                (
                    AES
                    (
                        forEncryption: false, 
                        key: key, 
                        data: HexToBytes(cipher)
                    )
                );
        }

        /// <summary>
        /// Core symmetric encryption function
        /// </summary>
        private static byte[] AES(bool forEncryption, string key, byte[] data)
        {
            byte[] hashed_key;
            using (var hasher = SHA256.Create())
                hashed_key = hasher.ComputeHash(Encoding.UTF8.GetBytes(key + "I like good hash, but only if it's well salted"));

            var cipher = new PaddedBufferedBlockCipher(new AesEngine(), new Pkcs7Padding());
            cipher.Init(forEncryption, new KeyParameter(hashed_key));

            return cipher.DoFinal(data);
        }
    }
}
