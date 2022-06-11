using System;
using System.Threading.Tasks;
using System.IO;
using System.Net.Sockets;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace msgfiles
{
    public static class ssl
    {
        public static Stream ConnectToSecureServer(string hostname, int port)
        {
            var client = new TcpClient(hostname, port);
            var stream = new SslStream(client.GetStream(), false, (object obj, X509Certificate cert, X509Chain chain, SslPolicyErrors errors) => true);
            stream.AuthenticateAsClient(hostname);
            return stream;
        }

        public async static Task<Stream> SecureRemoteConnectionAsync(TcpClient client, X509Certificate cert)
        {
            var stream = new SslStream(client.GetStream(), false, (object obj, X509Certificate cert2, X509Chain chain, SslPolicyErrors errors) => true);
            await stream.AuthenticateAsServerAsync(cert, false, SslProtocols.Tls13, false).ConfigureAwait(false);
            return stream;
        }

        public static X509Certificate GenCert(string hostname)
        {
            using (RSA rsa = RSA.Create(4096))
            {
                var sanBuilder = new SubjectAlternativeNameBuilder();
                sanBuilder.AddDnsName(hostname);

                var distinguishedName = new X500DistinguishedName($"CN={hostname}");
                var request = new CertificateRequest(distinguishedName, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

                request.CertificateExtensions.Add(
                    new X509KeyUsageExtension(X509KeyUsageFlags.DataEncipherment | X509KeyUsageFlags.KeyEncipherment | X509KeyUsageFlags.DigitalSignature, false));

                request.CertificateExtensions.Add(
                   new X509EnhancedKeyUsageExtension(
                       new OidCollection { new Oid("1.2.840.113549.1.1.1") }, false));

                request.CertificateExtensions.Add(sanBuilder.Build());

                var certificate = request.CreateSelfSigned(new DateTimeOffset(DateTime.UtcNow.AddDays(-1)), new DateTimeOffset(DateTime.UtcNow.AddDays(3650)));
                certificate.FriendlyName = hostname;

                return new X509Certificate2(certificate.Export(X509ContentType.Pfx, "password"), "password", X509KeyStorageFlags.MachineKeySet);
            }
        }
    }
}
