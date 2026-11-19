using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace IdP.Features.IdentityProviders;

/// <summary>
/// Registers an OpenID Connect authentication handler for every enabled external provider of the database, and
/// keeps the registrations in step with it: at startup, after a change through the administration API, and
/// periodically, for the changes made by another instance.
/// </summary>
public sealed class DynamicSchemeManager(
    IdPMongoContext context,
    IAuthenticationSchemeProvider schemes,
    IOptionsMonitorCache<OpenIdConnectOptions> optionsCache,
    IEnumerable<IPostConfigureOptions<OpenIdConnectOptions>> postConfigures,
    IDataProtectionProvider dataProtection,
    ILogger<DynamicSchemeManager> logger) : BackgroundService
{
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromMinutes(1);
    private readonly IDataProtector _secrets = dataProtection.CreateProtector("IdP.IdentityProviders.ClientSecret");
    private readonly SemaphoreSlim _sync = new(1, 1);
    private readonly Dictionary<string, DateTime> _registered = new(StringComparer.Ordinal);

    public string ProtectSecret(string secret) => _secrets.Protect(secret);

    /// <summary>The enabled providers, in order, for the login page and the account console.</summary>
    public Task<List<IdentityProviderDocument>> GetEnabledAsync(CancellationToken cancellationToken = default)
        => context.IdentityProviders.Find(p => p.Enabled).SortBy(p => p.Order).ThenBy(p => p.DisplayName).ToListAsync(cancellationToken);

    /// <summary>Applies the database to the registered schemes: adds, replaces and removes handlers as needed.</summary>
    public async Task SyncAsync(CancellationToken cancellationToken = default)
    {
        await _sync.WaitAsync(cancellationToken);
        try
        {
            var providers = await context.IdentityProviders.Find(FilterDefinition<IdentityProviderDocument>.Empty).ToListAsync(cancellationToken);
            var wanted = providers.Where(p => p.Enabled).ToDictionary(p => p.Alias, StringComparer.Ordinal);

            foreach (var alias in _registered.Keys.ToList())
            {
                if (!wanted.TryGetValue(alias, out var provider) || provider.UpdatedAt != _registered[alias])
                {
                    schemes.RemoveScheme(alias);
                    optionsCache.TryRemove(alias);
                    _registered.Remove(alias);
                    logger.LogInformation("External provider {Alias} unregistered", alias);
                }
            }

            foreach (var provider in wanted.Values)
            {
                if (_registered.ContainsKey(provider.Alias))
                {
                    continue;
                }

                try
                {
                    var options = Build(provider);
                    foreach (var postConfigure in postConfigures)
                    {
                        postConfigure.PostConfigure(provider.Alias, options);
                    }

                    options.Validate();
                    optionsCache.TryAdd(provider.Alias, options);
                    schemes.AddScheme(new AuthenticationScheme(provider.Alias, provider.DisplayName, typeof(OpenIdConnectHandler)));
                    _registered[provider.Alias] = provider.UpdatedAt;
                    logger.LogInformation("External provider {Alias} registered ({Authority})", provider.Alias, provider.Authority);
                }
                catch (Exception ex)
                {
                    optionsCache.TryRemove(provider.Alias);
                    logger.LogError(ex, "External provider {Alias} could not be registered", provider.Alias);
                }
            }
        }
        finally
        {
            _sync.Release();
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SyncAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "The external providers could not be read");
            }

            try
            {
                await Task.Delay(RefreshInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    private OpenIdConnectOptions Build(IdentityProviderDocument provider)
    {
        var options = new OpenIdConnectOptions
        {
            SignInScheme = OidcServerConstants.ExternalCookieAuthenticationScheme,
            Authority = provider.Authority,
            ClientId = provider.ClientId,
            ResponseType = OpenIdConnectResponseType.Code,
            ResponseMode = OpenIdConnectResponseMode.Query,
            UsePkce = provider.UsePkce,
            GetClaimsFromUserInfoEndpoint = provider.GetClaimsFromUserInfoEndpoint,
            SaveTokens = false,
            MapInboundClaims = false,
            CallbackPath = provider.CallbackPath,
            SignedOutCallbackPath = provider.SignedOutCallbackPath,
            RemoteSignOutPath = $"/external/remote-signout-{provider.Alias}",
            RequireHttpsMetadata = provider.Authority.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
        };

        if (!string.IsNullOrEmpty(provider.ClientSecret))
        {
            options.ClientSecret = _secrets.Unprotect(provider.ClientSecret);
        }

        options.Scope.Clear();
        foreach (var scope in provider.Scopes.Where(s => !string.IsNullOrWhiteSpace(s)))
        {
            options.Scope.Add(scope);
        }

        options.TokenValidationParameters.NameClaimType = JwtClaimTypes.Name;
        options.TokenValidationParameters.RoleClaimType = JwtClaimTypes.Role;

        // A failure at the provider comes back to the login page as a message instead of an exception page.
        options.Events.OnRemoteFailure = context =>
        {
            var message = context.Failure?.Message ?? "The external sign-in failed.";
            context.Response.Redirect("/login?error=" + Uri.EscapeDataString($"{provider.DisplayName}: {message}"));
            context.HandleResponse();
            return Task.CompletedTask;
        };

        return options;
    }
}
