using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

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

        public static async Task<Stream> ConnectToSecureServer(string hostname, int port)
        {
            var client = new TcpClient(hostname, port);
            var stream = new SslStream(client.GetStream(), false, (object obj, X509Certificate cert, X509Chain chain, SslPolicyErrors errors) => true);
            await stream.AuthenticateAsClientAsync(hostname).ConfigureAwait(false);
            return stream;
        }

        public async static Task<Stream> SecureRemoteConnectionAsync(TcpClient client, X509Certificate cert)
        {
            var stream = new SslStream(client.GetStream(), false, (object obj, X509Certificate cert2, X509Chain chain, SslPolicyErrors errors) => true);
            await stream.AuthenticateAsServerAsync(cert, false, SslProtocols.Tls13, false).ConfigureAwait(false);
            return stream;
        }

        public static async Task<string> ReadHeaderAsync(Stream stream, int maxHeaderSize)
        {
            byte[] bytes = new byte[4];
            int read = await stream.ReadAsync(bytes, 0, 4).ConfigureAwait(false);
            if (read != 4)
                return "";

            int num = BitConverter.ToInt32(bytes, 0);
            num = IPAddress.NetworkToHostOrder(num);
            if (num > maxHeaderSize)
                return "";

            MemoryStream header_buffer = new MemoryStream(num);
            int read_yet = 0;
            while (read_yet < num)
            {
                int to_read = num - read_yet;
                int new_read = await stream.ReadAsync(header_buffer.GetBuffer(), read_yet, to_read).ConfigureAwait(false);
                if (new_read <= 0)
                    return "";
                else
                    read_yet += new_read;
            }
            return Encoding.UTF8.GetString(header_buffer.GetBuffer(), 0, num);
        }
    }
}
