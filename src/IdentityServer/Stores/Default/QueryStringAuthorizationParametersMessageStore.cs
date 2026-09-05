namespace IdentityServer.Stores;

internal class QueryStringAuthorizationParametersMessageStore : IAuthorizationParametersMessageStore
{
    private readonly TimeProvider _clock;

    public QueryStringAuthorizationParametersMessageStore(TimeProvider clock)
    {
        _clock = clock;
    }

    public Task<string> WriteAsync(Message<IDictionary<string, string[]>> message)
    {
        var queryString = message.Data.FromFullDictionary().ToQueryString();
        return Task.FromResult(queryString);
    }

    public Task<Message<IDictionary<string, string[]>>> ReadAsync(string? id)
    {
        var values = id.ReadQueryStringAsNameValueCollection();
        var msg = new Message<IDictionary<string, string[]>>(values.ToFullDictionary(), _clock.GetUtcNow().UtcDateTime);
        return Task.FromResult(msg);
    }

    public Task DeleteAsync(string? id)
    {
        return Task.CompletedTask;
    }
}
