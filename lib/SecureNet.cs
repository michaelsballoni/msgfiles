using System.Net;
using System.Net.Security;
using System.Net.Sockets;

using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

using System.Text;

using Newtonsoft.Json;

namespace msgfiles
{
    public static class SecureNet
    {
        public static X509Certificate GenCert(string hostname)
        {
            using (RSA rsa = RSA.Create(4096))
            {
                var sanBuilder = new SubjectAlternativeNameBuilder();
                sanBuilder.AddDnsName(hostname);

                var distinguishedName = new X500DistinguishedName($"CN={hostname}");
                var request = new CertificateRequest(distinguishedName, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

                request.CertificateExtensions.Add(sanBuilder.Build());

                var certificate = request.CreateSelfSigned(new DateTimeOffset(DateTime.UtcNow.AddDays(-1)), new DateTimeOffset(DateTime.UtcNow.AddDays(3650)));
                return new X509Certificate2(certificate.Export(X509ContentType.Pfx, "password"), "password", X509KeyStorageFlags.MachineKeySet);
            }
        }

        public static async Task<Stream> ConnectToSecureServer(TcpClient client)
        {
            var stream = new SslStream(client.GetStream(), false, (object obj, X509Certificate cert, X509Chain chain, SslPolicyErrors errors) => true);
            await stream.AuthenticateAsClientAsync("msgfiles.io").ConfigureAwait(false);
            return stream;
        }

        public async static Task<Stream> SecureRemoteConnectionAsync(TcpClient client, X509Certificate cert)
        {
            var stream = new SslStream(client.GetStream(), false, (object obj, X509Certificate cert2, X509Chain chain, SslPolicyErrors errors) => true);
            await stream.AuthenticateAsServerAsync(cert, false, SslProtocols.Tls13, false).ConfigureAwait(false);
            return stream;
        }

        public static async Task SendHeadersAsync(Stream stream, Dictionary<string, string> headers)
        {
            string json = JsonConvert.SerializeObject(headers);

            byte[] json_bytes = Encoding.UTF8.GetBytes(json);
            if (json_bytes.Length > 64 * 1024)
                throw new Exception("Too much to send");

            byte[] num_bytes = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(json_bytes.Length));
            await stream.WriteAsync(num_bytes, 0, num_bytes.Length).ConfigureAwait(false);
            await stream.WriteAsync(json_bytes, 0, json_bytes.Length).ConfigureAwait(false);
        }

        public static async Task<Dictionary<string, string>> ReadHeadersAsync(Stream stream)
        {
            byte[] num_bytes = new byte[4];
            int read = await stream.ReadAsync(num_bytes, 0, 4).ConfigureAwait(false);
            if (read == 0)
                throw new Exception("Connection closed");

            int bytes_length = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(num_bytes, 0));
            if (bytes_length > 64 * 1024)
                throw new Exception("Too much to read");

            byte[] header_bytes = new byte[bytes_length];
            int read_yet = 0;
            while (read_yet < bytes_length)
            {
                int to_read = bytes_length - read_yet;
                int new_read = await stream.ReadAsync(header_bytes, read_yet, to_read).ConfigureAwait(false);
                if (new_read <= 0)
                    throw new Exception("Connection closed");
                else
                    read_yet += new_read;
            }

            string json = Encoding.UTF8.GetString(header_bytes, 0, bytes_length);
            return JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
        }
    }
}
