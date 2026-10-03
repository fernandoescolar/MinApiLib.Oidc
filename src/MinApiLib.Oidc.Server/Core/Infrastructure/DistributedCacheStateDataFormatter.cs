

namespace MinApiLib.Oidc.Server.Core.Infrastructure;

/// <summary>
/// State formatter using IDistributedCache
/// </summary>
public class DistributedCacheStateDataFormatter : ISecureDataFormat<AuthenticationProperties>
{
    private readonly IHttpContextAccessor _httpContext;
    private readonly string _name;

    /// <summary>
    /// Initializes a new instance of the <see cref="DistributedCacheStateDataFormatter"/> class.
    /// </summary>
    /// <param name="httpContext">The HTTP context accessor.</param>
    /// <param name="name">The scheme name.</param>
    public DistributedCacheStateDataFormatter(IHttpContextAccessor httpContext, string name)
    {
        _httpContext = httpContext;
        _name = name;
    }

    private string CacheKeyPrefix => "DistributedCacheStateDataFormatter";

    // only used while handling a request, so HttpContext is always available
    private HttpContext HttpContext => _httpContext.HttpContext!;

    private IDistributedCache Cache => HttpContext.RequestServices.GetRequiredService<IDistributedCache>();
    private IDataProtector Protector => HttpContext.RequestServices.GetRequiredService<IDataProtectionProvider>().CreateProtector(CacheKeyPrefix, _name);

    /// <summary>
    /// Protects the specified data.
    /// </summary>
    /// <param name="data">The data.</param>
    /// <returns></returns>
    public string Protect(AuthenticationProperties data)
    {
        return Protect(data, null);
    }

    /// <summary>
    /// Protects the specified data.
    /// </summary>
    /// <param name="data">The data.</param>
    /// <param name="purpose">The purpose.</param>
    /// <returns></returns>
    public string Protect(AuthenticationProperties data, string? purpose)
    {
        var key = Guid.NewGuid().ToString();
        var cacheKey = $"{CacheKeyPrefix}-{_name}-{purpose}-{key}";
        var json = ObjectSerializer.ToString(data.Items);

        var options = new DistributedCacheEntryOptions();
        if (data.ExpiresUtc.HasValue)
        {
            options.SetAbsoluteExpiration(data.ExpiresUtc.Value);
        }
        else
        {
            options.SetSlidingExpiration(Constants.DefaultCacheDuration);
        }

        Cache.SetString(cacheKey, json, options);

        return Protector.Protect(key);
    }

    /// <summary>
    /// Unprotects the specified protected text.
    /// </summary>
    /// <param name="protectedText">The protected text.</param>
    /// <returns></returns>
    public AuthenticationProperties? Unprotect(string? protectedText)
    {
        return Unprotect(protectedText, null);
    }

    /// <summary>
    /// Unprotects the specified protected text.
    /// </summary>
    /// <param name="protectedText">The protected text.</param>
    /// <param name="purpose">The purpose.</param>
    /// <returns></returns>
    public AuthenticationProperties? Unprotect(string? protectedText, string? purpose)
    {
        if (String.IsNullOrWhiteSpace(protectedText))
        {
            return null;
        }

        var key = Protector.Unprotect(protectedText);
        var cacheKey = $"{CacheKeyPrefix}-{_name}-{purpose}-{key}";
        var json = Cache.GetString(cacheKey);

        if (json == null)
        {
            return null;
        }

        var items = ObjectSerializer.FromString<Dictionary<string, string?>>(json);
        // the cached json is always the serialized (non-null) Items dictionary written by Protect
        var props = new AuthenticationProperties(items!);
        return props;
    }
}
