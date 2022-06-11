using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Security.Cryptography.X509Certificates;

namespace msgfiles
{
    public static class serve
    {
        public async static Task ServePortAsync(int port, X509Certificate cert)
        {
            var listener = new TcpListener(IPAddress.Loopback, port);
            listener.Start();
            while (true)
            {
                var client = await listener.AcceptTcpClientAsync().ConfigureAwait(false);
                var stream = await ssl.SecureRemoteConnectionAsync(client, cert).ConfigureAwait(false);
                _ = Task.Run(async () => await HandleClientAsync(stream));
            }
        }

        private async static Task HandleClientAsync(Stream client)
        {
            try
            {

            }
            finally
            {
                client.Dispose();
            }
        }
    }
}
