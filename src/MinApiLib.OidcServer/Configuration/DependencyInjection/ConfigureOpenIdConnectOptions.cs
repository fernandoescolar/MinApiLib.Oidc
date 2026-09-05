namespace MinApiLib.OidcServer.Configuration;

internal class ConfigureOpenIdConnectOptions : IPostConfigureOptions<OpenIdConnectOptions>
{
    private string[] _schemes;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ConfigureOpenIdConnectOptions(string[] schemes, IHttpContextAccessor httpContextAccessor)
    {
        _schemes = schemes ?? throw new ArgumentNullException(nameof(schemes));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    public void PostConfigure(string? name, OpenIdConnectOptions options)
    {
        if (_schemes.Length == 0 || _schemes.Contains(name))
        {
            // the options framework always passes the scheme name when post-configuring a named instance
            options.StateDataFormat = new DistributedCacheStateDataFormatter(_httpContextAccessor, name!);
        }
    }
}
