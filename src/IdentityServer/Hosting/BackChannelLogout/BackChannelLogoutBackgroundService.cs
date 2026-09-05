using Microsoft.Extensions.Hosting;

namespace IdentityServer.Hosting;

/// <summary>
/// Sends queued back-channel logout notifications through <see cref="IBackChannelLogoutService"/>.
/// </summary>
internal sealed class BackChannelLogoutBackgroundService : BackgroundService
{
    private readonly BackChannelLogoutQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<BackChannelLogoutBackgroundService> _logger;

    public BackChannelLogoutBackgroundService(
        BackChannelLogoutQueue queue,
        IServiceScopeFactory scopeFactory,
        IHttpContextAccessor httpContextAccessor,
        ILogger<BackChannelLogoutBackgroundService> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await foreach (var item in _queue.Reader.ReadAllAsync(stoppingToken))
            {
                await SendAsync(item);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Host shutdown.
        }
    }

    private async Task SendAsync(BackChannelLogoutWorkItem item)
    {
        using var scope = _scopeFactory.CreateScope();
        _httpContextAccessor.HttpContext = item.CreateHttpContext(scope.ServiceProvider);
        try
        {
            var service = scope.ServiceProvider.GetRequiredService<IBackChannelLogoutService>();
            await service.SendLogoutNotificationsAsync(item.Context);
        }
        catch (Exception ex)
        {
            ResponseAndHostingLog.BackChannelLogoutFailed(_logger, ex, item.Context.SessionId);
        }
        finally
        {
            _httpContextAccessor.HttpContext = null;
        }
    }
}
