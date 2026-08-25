using System.Security.Cryptography;
using Microsoft.Extensions.Caching.Memory;

namespace Nedis.Server.Common;

// Short-lived, one-time tickets for the WebSocket/SignalR handshake.
// Concept: the actual JWT never leaves the Authorization header;
// only the ticket appears in the handshake URL-it is dereferenced
// right here on the server and immediately invalidated (single-use).
// Even if the ticket leaks into reverse-proxy logs, it becomes
// useless within seconds and cannot be replayed.
//
// In-memory by design: if running multiple application instances
// behind a load balancer, it would need to be moved to a shared
// cache (such as the Redis instance used for the SignalR backplane)—
// but while there is only a single instance, a simple dictionary suffices.
public class ConnectionTicketStore(IMemoryCache cache)
{

    public string Issue(string accessToken, TimeSpan lifetime)
    {
        var ticket = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        cache.Set(ticket, accessToken, lifetime);

        return ticket;
    }

    public bool TryConsume(string ticket, out string? accessToken)
    {
        if (cache.TryGetValue(ticket, out var value) && value is string token)
        {
            cache.Remove(ticket);
            accessToken = token;
            return true;
        }

        accessToken = null;
        return false;
    }
}
