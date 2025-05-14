using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using DnsClient;
using DnsProxy.Models;

namespace DnsProxy.Services
{
    public interface IHttpClientPerServerService
    {
        HttpClient GetOrCreate(DnsServerEntry server);
    }

    public class HttpClientPerServerService : IHttpClientPerServerService
    {
        private readonly ConcurrentDictionary<string, HttpClient> _clients = new();

        public HttpClient GetOrCreate(DnsServerEntry server)
        {
            if (string.IsNullOrWhiteSpace(server.Address))
                throw new ArgumentException("Server address is null or empty");

            return _clients.GetOrAdd(server.Address, _ =>
            {
                // Используем Google DNS или любой другой, можно вынести в конфиг
                var dnsServer = "8.8.8.8";

                var lookup = new LookupClient(new LookupClientOptions(IPAddress.Parse(dnsServer))
                {
                    UseCache = true,
                    UseTcpFallback = true,
                });

                var handler = new SocketsHttpHandler
                {
                    ConnectCallback = async (context, ct) =>
                    {
                        var host = context.DnsEndPoint.Host;
                        var port = context.DnsEndPoint.Port;

                        var result = await lookup.GetHostEntryAsync(host);
                        var ip = result.AddressList
                            .FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork);

                        if (ip is null)
                            throw new Exception($"Не удалось разрешить хост: {host}");

                        var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                        await socket.ConnectAsync(ip, port);
                        return new NetworkStream(socket, ownsSocket: true);
                    },

                    PooledConnectionLifetime = TimeSpan.FromMinutes(5),
                    MaxConnectionsPerServer = 10,
                    ConnectTimeout = TimeSpan.FromSeconds(2),
                    AutomaticDecompression = DecompressionMethods.All,
                };

                return new HttpClient(handler)
                {
                    Timeout = TimeSpan.FromSeconds(3)
                };
            });
        }
    }
}
