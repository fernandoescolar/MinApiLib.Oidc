namespace IdentityServer.Services;

/// <summary>
/// Default implementation of logout notification service.
/// </summary>
public class LogoutNotificationService : ILogoutNotificationService
{
    private readonly IClientStore _clientStore;
    private readonly IIssuerNameService _issuerNameService;
    private readonly ILogger<LogoutNotificationService> _logger;

    /// <summary>
    /// Ctor.
    /// </summary>
    public LogoutNotificationService(
        IClientStore clientStore,
        IIssuerNameService issuerNameService,
        ILogger<LogoutNotificationService> logger)
    {
        _clientStore = clientStore;
        _issuerNameService = issuerNameService;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<string>> GetFrontChannelLogoutNotificationsUrlsAsync(LogoutNotificationContext context)
    {
        var frontChannelUrls = new List<string>();
        foreach (var clientId in context.ClientIds)
        {
            var client = await _clientStore.FindEnabledClientByIdAsync(clientId);
            if (client != null)
            {
                if (client.FrontChannelLogoutUri.IsPresent())
                {
                    var url = client.FrontChannelLogoutUri;

                    if (client.ProtocolType == IdentityServerConstants.ProtocolTypes.OpenIdConnect)
                    {
                        if (client.FrontChannelLogoutSessionRequired)
                        {
                            url = url.AddQueryString(OidcConstants.EndSessionRequest.Sid, context.SessionId ?? throw new InvalidOperationException("Front-channel logout with session required needs a session id."));
                            url = url.AddQueryString(OidcConstants.EndSessionRequest.Issuer, await _issuerNameService.GetCurrentAsync());
                        }
                    }
                    else if (client.ProtocolType == IdentityServerConstants.ProtocolTypes.WsFederation)
                    {
                        url = url.AddQueryString(Constants.WsFedSignOut.LogoutUriParameterName, Constants.WsFedSignOut.LogoutUriParameterValue);
                    }

                    frontChannelUrls.Add(url);
                }
            }
        }

        if (frontChannelUrls.Count > 0)
        {
            var msg = frontChannelUrls.Aggregate((x, y) => x + ", " + y);
            ServicesLog.FrontChannelLogoutUrls(_logger, msg);
        }
        else
        {
            ServicesLog.NoFrontChannelLogoutUrls(_logger);
        }

        return frontChannelUrls;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<BackChannelLogoutRequest>> GetBackChannelLogoutNotificationsAsync(LogoutNotificationContext context)
    {
        var backChannelLogouts = new List<BackChannelLogoutRequest>();
        foreach (var clientId in context.ClientIds)
        {
            var client = await _clientStore.FindEnabledClientByIdAsync(clientId);
            if (client != null)
            {
                if (client.BackChannelLogoutUri.IsPresent())
                {
                    var back = new BackChannelLogoutRequest
                    {
                        ClientId = clientId,
                        LogoutUri = client.BackChannelLogoutUri,
                        SubjectId = context.SubjectId,
                        SessionId = context.SessionId,
                        SessionIdRequired = client.BackChannelLogoutSessionRequired
                    };

                    backChannelLogouts.Add(back);
                }
            }
        }

        if (backChannelLogouts.Count > 0)
        {
            var msg = backChannelLogouts.Select(x => x.LogoutUri).Aggregate((x, y) => x + ", " + y);
            ServicesLog.BackChannelLogoutUrls(_logger, msg);
        }
        else
        {
            ServicesLog.NoBackChannelLogoutUrls(_logger);
        }

        return backChannelLogouts;
    }
}
