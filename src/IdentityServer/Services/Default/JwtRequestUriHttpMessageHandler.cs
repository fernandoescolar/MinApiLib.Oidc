using System.Net.Sockets;

namespace IdentityServer.Services;

/// <summary>
/// Builds the primary HTTP handler used to fetch <c>request_uri</c> values. The handler does not follow
/// redirects and only connects to public IP addresses: host names are resolved at connect time and every
/// private, loopback, link-local or reserved address is refused, which also defeats DNS rebinding.
/// </summary>
internal static class JwtRequestUriHttpMessageHandler
{
    public static SocketsHttpHandler Create() => new()
    {
        AllowAutoRedirect = false,
        ConnectCallback = ConnectToPublicAddressAsync
    };

    internal static async ValueTask<Stream> ConnectToPublicAddressAsync(SocketsHttpConnectionContext context, CancellationToken cancellationToken)
    {
        var host = context.DnsEndPoint.Host;
        var addresses = IPAddress.TryParse(host, out var literal)
            ? [literal]
            : await Dns.GetHostAddressesAsync(host, cancellationToken);

        var allowed = addresses.Where(address => !IsPrivateOrReservedIp(address)).ToArray();
        if (allowed.Length == 0)
        {
            throw new HttpRequestException($"request_uri host '{host}' does not resolve to a public IP address.");
        }

        var socket = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
        try
        {
            await socket.ConnectAsync(allowed, context.DnsEndPoint.Port, cancellationToken);
            return new NetworkStream(socket, ownsSocket: true);
        }
        catch
        {
            socket.Dispose();
            throw;
        }
    }

    internal static bool IsPrivateOrReservedIp(IPAddress ip)
    {
        if (ip.IsIPv4MappedToIPv6)
        {
            return IsPrivateOrReservedIp(ip.MapToIPv4());
        }

        var bytes = ip.GetAddressBytes();

        if (ip.AddressFamily == AddressFamily.InterNetworkV6)
        {
            if (ip.Equals(IPAddress.IPv6Loopback) || ip.Equals(IPAddress.IPv6None) ||
                ip.IsIPv6LinkLocal || ip.IsIPv6SiteLocal || ip.IsIPv6Multicast)
            {
                return true;
            }

            if ((bytes[0] & 0xFE) == 0xFC)
            {
                return true; // fc00::/7 unique local
            }

            // 64:ff9b::/96 NAT64: check the embedded IPv4 address.
            if (bytes[0] == 0x00 && bytes[1] == 0x64 && bytes[2] == 0xFF && bytes[3] == 0x9B &&
                bytes.Skip(4).Take(8).All(b => b == 0))
            {
                return IsPrivateOrReservedIp(new IPAddress(bytes.Skip(12).ToArray()));
            }

            return false;
        }

        return bytes[0] switch
        {
            0 => true,                                                 // 0.0.0.0/8 this network
            10 => true,                                                // 10.0.0.0/8
            100 when (bytes[1] & 0xC0) == 64 => true,                  // 100.64.0.0/10 shared address space
            127 => true,                                               // 127.0.0.0/8 loopback
            169 when bytes[1] == 254 => true,                          // 169.254.0.0/16 link-local, cloud metadata
            172 when bytes[1] is >= 16 and <= 31 => true,              // 172.16.0.0/12
            192 when bytes[1] == 0 && bytes[2] is 0 or 2 => true,      // 192.0.0.0/24 IETF, 192.0.2.0/24 documentation
            192 when bytes[1] == 168 => true,                          // 192.168.0.0/16
            198 when bytes[1] is 18 or 19 => true,                     // 198.18.0.0/15 benchmarking
            198 when bytes[1] == 51 && bytes[2] == 100 => true,        // 198.51.100.0/24 documentation
            203 when bytes[1] == 0 && bytes[2] == 113 => true,         // 203.0.113.0/24 documentation
            >= 224 => true,                                            // 224.0.0.0/4 multicast, 240.0.0.0/4 reserved, broadcast
            _ => false
        };
    }
}
