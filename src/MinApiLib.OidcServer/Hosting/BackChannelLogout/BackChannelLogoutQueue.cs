using System.Threading.Channels;

namespace MinApiLib.OidcServer.Hosting;

/// <summary>
/// Queue of back-channel logout notifications sent by <see cref="BackChannelLogoutBackgroundService"/>, so that
/// slow or unreachable clients do not delay the response to the user's sign-out request.
/// </summary>
[RegisterInIServiceCollection(ServiceLifetime = ServiceLifetime.Singleton)]
internal sealed class BackChannelLogoutQueue
{
    private const int Capacity = 10_000;

    private readonly Channel<BackChannelLogoutWorkItem> _channel = Channel.CreateBounded<BackChannelLogoutWorkItem>(
        new BoundedChannelOptions(Capacity) { SingleReader = true, FullMode = BoundedChannelFullMode.Wait });

    public ChannelReader<BackChannelLogoutWorkItem> Reader => _channel.Reader;

    /// <summary>
    /// Queues the notifications. Returns <c>false</c> when the queue is full; the caller then sends them inline.
    /// </summary>
    public bool TryEnqueue(BackChannelLogoutWorkItem item) => _channel.Writer.TryWrite(item);
}
