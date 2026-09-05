namespace IdentityServer.Services;

/// <summary>
/// Models making HTTP requests for back-channel logout notification.
/// </summary>
public class DefaultBackChannelLogoutHttpClient : IBackChannelLogoutHttpClient
{
    private readonly HttpClient _client;
    private readonly ILogger<DefaultBackChannelLogoutHttpClient> _logger;

    /// <summary>
    /// Constructor for BackChannelLogoutHttpClient.
    /// </summary>
    /// <param name="client"></param>
    /// <param name="loggerFactory"></param>
    public DefaultBackChannelLogoutHttpClient(HttpClient client, ILoggerFactory loggerFactory)
    {
        _client = client;
        _logger = loggerFactory.CreateLogger<DefaultBackChannelLogoutHttpClient>();
    }

    /// <summary>
    /// Posts the payload to the url.
    /// </summary>
    /// <param name="url"></param>
    /// <param name="payload"></param>
    /// <returns></returns>
    public async Task PostAsync(string url, Dictionary<string, string> payload)
    {
        try
        {
            var response = await _client.PostAsync(url, new FormUrlEncodedContent(payload));
            if (response.IsSuccessStatusCode)
            {
                ServicesLog.BackChannelLogoutResponse(_logger, url, (int)response.StatusCode);
            }
            else
            {
                ServicesLog.BackChannelLogoutResponseFailure(_logger, url, (int)response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            ServicesLog.BackChannelLogoutException(_logger, ex, url);
        }
    }
}
