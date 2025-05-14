using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using ARSoft.Tools.Net;
using ARSoft.Tools.Net.Dns;
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
                var handler = new SocketsHttpHandler
                {
                    ConnectCallback = async (context, ct) =>
                    {
                        var host = context.DnsEndPoint.Host;
                        var port = context.DnsEndPoint.Port;

                        var ip = await ResolveWithArsoft(host) ?? throw new Exception($"Не удалось разрешить хост: {host}");
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

        private static async Task<IPAddress?> ResolveWithArsoft(string host)
        {
            var dnsServer = IPAddress.Parse("8.8.8.8");

            var resolver = new DnsStubResolver(new[] { dnsServer }, 3000);

            var response = await resolver.ResolveAsync<ARecord>(
                DomainName.Parse(host),
                RecordType.A,
                RecordClass.INet
            );

            return response.FirstOrDefault()?.Address;
        }
    }
}
