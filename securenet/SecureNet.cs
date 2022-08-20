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
        public static int MaxSendObjectBytes = 64 * 1024;
        public static int MaxReadObjectBytes = 64 * 1024;

        public static X509Certificate GenCert()
        {
            using (RSA rsa = RSA.Create(4096))
            {
                var distinguishedName = new X500DistinguishedName($"CN=msgfiles.io");
                var request = new CertificateRequest(distinguishedName, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(3650));
                return new X509Certificate2(certificate.Export(X509ContentType.Pfx, "password"), "password", X509KeyStorageFlags.MachineKeySet);
            }
        }

        public static Stream SecureConnectionToServer(TcpClient client)
        {
            var client_stream = client.GetStream();
            var ssl_stream = 
                new SslStream
                (
                    client_stream,
                    false,
                    (object obj, X509Certificate? cert, X509Chain? chain, SslPolicyErrors errors) => true
                );
            ssl_stream.AuthenticateAsClient("msgfiles.io");
            if (!ssl_stream.IsAuthenticated)
                throw new NetworkException("Connection to server not authenticated");
            return ssl_stream;
        }

        public async static Task<Stream> SecureConnectionFromClient(TcpClient client, X509Certificate cert)
        {
            var client_stream = client.GetStream();
            var ssl_stream = new SslStream(client_stream, false, (object obj, X509Certificate? cert2, X509Chain? chain, SslPolicyErrors errors) => true);
            await ssl_stream.AuthenticateAsServerAsync(cert).ConfigureAwait(false);
            if (!ssl_stream.IsAuthenticated)
                throw new NetworkException("Connection from client not authenticated");
            return ssl_stream;
        }

        public static void SendObject<T>(Stream stream, T headers)
        {
            string json = JsonConvert.SerializeObject(headers);

            byte[] json_bytes = Utils.Compress(Encoding.UTF8.GetBytes(json));
            if (json_bytes.Length > MaxSendObjectBytes)
                throw new InputException("Too much to send");

            byte[] num_bytes = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(json_bytes.Length));

            using (var buffer = Utils.CombineArrays(num_bytes, json_bytes))
                stream.Write(buffer.GetBuffer(), 0, (int)buffer.Length);
        }

        public static async Task SendObjectAsync<T>(Stream stream, T headers)
        {
            string json = JsonConvert.SerializeObject(headers);

            byte[] json_bytes = Utils.Compress(Encoding.UTF8.GetBytes(json));
            if (json_bytes.Length > MaxSendObjectBytes)
                throw new InputException("Too much to send");

            byte[] num_bytes = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(json_bytes.Length));

            using (var buffer = Utils.CombineArrays(num_bytes, json_bytes))
                await stream.WriteAsync(buffer.GetBuffer(), 0, (int)buffer.Length).ConfigureAwait(false);
        }

        public static T ReadObject<T>(Stream stream)
        {
            byte[] num_bytes = new byte[4];
            if (stream.Read(num_bytes, 0, num_bytes.Length) != num_bytes.Length)
                throw new SocketException();

            int bytes_length = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(num_bytes, 0));
            if (bytes_length > MaxReadObjectBytes)
                throw new InputException("Too much to read");

            byte[] header_bytes = new byte[bytes_length];
            int read_yet = 0;
            while (read_yet < bytes_length)
            {
                int to_read = bytes_length - read_yet;
                int new_read = stream.Read(header_bytes, read_yet, to_read);
                if (new_read <= 0)
                    throw new NetworkException("Connection closed");
                else
                    read_yet += new_read;
            }

            string json = Encoding.UTF8.GetString(Utils.Decompress(header_bytes, bytes_length));
            var obj = JsonConvert.DeserializeObject<T>(json);
            if (obj == null)
                throw new InputException("Input did not parse");
            else
                return obj;
        }

        public static async Task<T> ReadObjectAsync<T>(Stream stream)
        {
            byte[] num_bytes = new byte[4];
            if (await stream.ReadAsync(num_bytes, 0, num_bytes.Length).ConfigureAwait(false) != num_bytes.Length)
                throw new SocketException();

            int bytes_length = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(num_bytes, 0));
            if (bytes_length > MaxReadObjectBytes)
                throw new InputException("Too much to read");

            byte[] header_bytes = new byte[bytes_length];
            int read_yet = 0;
            while (read_yet < bytes_length)
            {
                int to_read = bytes_length - read_yet;
                int new_read = await stream.ReadAsync(header_bytes, read_yet, to_read).ConfigureAwait(false);
                if (new_read <= 0)
                    throw new NetworkException("Connection closed");
                else
                    read_yet += new_read;
            }

            string json = Encoding.UTF8.GetString(Utils.Decompress(header_bytes, bytes_length));
            var obj = JsonConvert.DeserializeObject<T>(json);
            if (obj == null)
                throw new InputException("Input did not parse");
            else
                return obj;
        }
    }
}
