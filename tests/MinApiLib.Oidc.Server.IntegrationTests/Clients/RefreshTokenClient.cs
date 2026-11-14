// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// Derived from IdentityServer4 4.1.2 (https://github.com/IdentityServer/IdentityServer4); modified for MinApiLib.Oidc.

using MinApiLib.Oidc.Server.IntegrationTests.Clients.Setup;

namespace MinApiLib.Oidc.Server.IntegrationTests.Clients;

/// <summary>
/// Integration tests for refresh token behaviour (rotation, reuse, replay protection, revocation).
/// Refresh tokens are bootstrapped via the 'custom' extension grant, which does not require
/// user credentials and supports offline_access.
/// </summary>
public class RefreshTokenClient : IDisposable
{
    private const string TokenEndpoint = "https://server/connect/token";
    private const string RevocationEndpoint = "https://server/connect/revocation";

    private readonly HttpClient _client;
    private readonly IHost _host;

    public void Dispose()
    {
        _client.Dispose();
        _host.Dispose();
        GC.SuppressFinalize(this);
    }

    public RefreshTokenClient()
    {
        _host = new HostBuilder()
            .ConfigureWebHost(webHost => webHost
                .UseTestServer()
                .UseStartup<Startup>())
            .Start();
        var server = _host.GetTestServer();

        _client = server.CreateClient();
    }

    // -------------------------------------------------------------------------
    // Bootstrap helper — obtains an initial refresh token via the custom grant
    // -------------------------------------------------------------------------

    private Task<TokenResponse> RequestInitialTokenAsync(string clientId, string scope = "openid api1 offline_access")
        => _client.RequestTokenAsync(new TokenRequest
        {
            Address = TokenEndpoint,
            GrantType = "custom",
            ClientId = clientId,
            ClientSecret = "secret",
            Parameters = { { "scope", scope }, { "custom_credential", "valid" } }
        });

    // -------------------------------------------------------------------------
    // Basic usage
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Using_a_valid_refresh_token_should_succeed()
    {
        var initial = await RequestInitialTokenAsync("client.refreshtoken");
        Assert.False(initial.IsError);
        Assert.NotNull(initial.RefreshToken);

        var refreshed = await _client.RequestRefreshTokenAsync(new RefreshTokenRequest
        {
            Address = TokenEndpoint,
            ClientId = "client.refreshtoken",
            ClientSecret = "secret",
            RefreshToken = initial.RefreshToken
        }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(refreshed.IsError);
        Assert.NotNull(refreshed.AccessToken);
    }

    // -------------------------------------------------------------------------
    // Rotation — one-time-only
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Refreshing_a_one_time_only_token_should_return_a_different_refresh_token()
    {
        var initial = await RequestInitialTokenAsync("client.refreshtoken");
        Assert.False(initial.IsError);

        var refreshed = await _client.RequestRefreshTokenAsync(new RefreshTokenRequest
        {
            Address = TokenEndpoint,
            ClientId = "client.refreshtoken",
            ClientSecret = "secret",
            RefreshToken = initial.RefreshToken
        }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(refreshed.IsError);
        Assert.NotNull(refreshed.RefreshToken);
        Assert.NotEqual(initial.RefreshToken, refreshed.RefreshToken);

        // The rotated token must be usable for a subsequent refresh
        var secondRefresh = await _client.RequestRefreshTokenAsync(new RefreshTokenRequest
        {
            Address = TokenEndpoint,
            ClientId = "client.refreshtoken",
            ClientSecret = "secret",
            RefreshToken = refreshed.RefreshToken
        }, cancellationToken: TestContext.Current.CancellationToken);
        Assert.False(secondRefresh.IsError);
        Assert.NotNull(secondRefresh.AccessToken);
    }

    // -------------------------------------------------------------------------
    // Reuse
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Refreshing_a_reuse_token_should_return_the_same_refresh_token()
    {
        var initial = await RequestInitialTokenAsync("client.refreshtoken.reuse");
        Assert.False(initial.IsError);

        var refreshed = await _client.RequestRefreshTokenAsync(new RefreshTokenRequest
        {
            Address = TokenEndpoint,
            ClientId = "client.refreshtoken.reuse",
            ClientSecret = "secret",
            RefreshToken = initial.RefreshToken
        }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(refreshed.IsError);
        Assert.Equal(initial.RefreshToken, refreshed.RefreshToken);
        Assert.NotNull(refreshed.AccessToken);
    }

    // -------------------------------------------------------------------------
    // Replay protection
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Replaying_a_rotated_refresh_token_should_fail_with_invalid_grant()
    {
        var initial = await RequestInitialTokenAsync("client.refreshtoken");
        Assert.False(initial.IsError);

        // First use — consumes the token and returns a new one
        var firstRefresh = await _client.RequestRefreshTokenAsync(new RefreshTokenRequest
        {
            Address = TokenEndpoint,
            ClientId = "client.refreshtoken",
            ClientSecret = "secret",
            RefreshToken = initial.RefreshToken
        }, cancellationToken: TestContext.Current.CancellationToken);
        Assert.False(firstRefresh.IsError);

        // Replay the already-consumed token — must be rejected
        var replay = await _client.RequestRefreshTokenAsync(new RefreshTokenRequest
        {
            Address = TokenEndpoint,
            ClientId = "client.refreshtoken",
            ClientSecret = "secret",
            RefreshToken = initial.RefreshToken
        }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(replay.IsError);
        Assert.Equal(OidcConstants.TokenErrors.InvalidGrant, replay.Error);
    }

    // -------------------------------------------------------------------------
    // Revocation
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Revoking_a_refresh_token_should_prevent_further_use()
    {
        var initial = await RequestInitialTokenAsync("client.refreshtoken");
        Assert.False(initial.IsError);

        var revocation = await _client.RevokeTokenAsync(new TokenRevocationRequest
        {
            Address = RevocationEndpoint,
            ClientId = "client.refreshtoken",
            ClientSecret = "secret",
            Token = initial.RefreshToken,
            TokenTypeHint = OidcConstants.TokenTypes.RefreshToken
        }, cancellationToken: TestContext.Current.CancellationToken);
        Assert.False(revocation.IsError);

        var afterRevocation = await _client.RequestRefreshTokenAsync(new RefreshTokenRequest
        {
            Address = TokenEndpoint,
            ClientId = "client.refreshtoken",
            ClientSecret = "secret",
            RefreshToken = initial.RefreshToken
        }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(afterRevocation.IsError);
        Assert.Equal(OidcConstants.TokenErrors.InvalidGrant, afterRevocation.Error);
    }

    // -------------------------------------------------------------------------
    // Reuse detection, jti and down-scoping (RFC 9700 section 4.14, RFC 6749 section 6)
    // -------------------------------------------------------------------------

    private Task<TokenResponse> RefreshAsync(string refreshToken, string scope = null)
        => _client.RequestRefreshTokenAsync(new RefreshTokenRequest
        {
            Address = TokenEndpoint,
            ClientId = "client.refreshtoken",
            ClientSecret = "secret",
            RefreshToken = refreshToken,
            Scope = scope
        }, cancellationToken: TestContext.Current.CancellationToken);

    [Fact]
    public async Task Replaying_a_rotated_refresh_token_should_revoke_the_newer_refresh_token()
    {
        var initial = await RequestInitialTokenAsync("client.refreshtoken");
        var rotated = await RefreshAsync(initial.RefreshToken);
        Assert.False(rotated.IsError);

        var replay = await RefreshAsync(initial.RefreshToken);
        Assert.True(replay.IsError);

        var afterReplay = await RefreshAsync(rotated.RefreshToken);
        Assert.True(afterReplay.IsError);
        Assert.Equal(OidcConstants.TokenErrors.InvalidGrant, afterReplay.Error);
    }

    [Fact]
    public async Task Refreshed_access_token_should_have_a_new_jti()
    {
        var initial = await RequestInitialTokenAsync("client.refreshtoken");
        var refreshed = await RefreshAsync(initial.RefreshToken);

        var handler = new JwtSecurityTokenHandler();
        Assert.NotEqual(handler.ReadJwtToken(initial.AccessToken).Id, handler.ReadJwtToken(refreshed.AccessToken).Id);
    }

    [Fact]
    public async Task Refresh_with_narrower_scope_should_down_scope_the_access_token_but_keep_the_grant()
    {
        var initial = await RequestInitialTokenAsync("client.refreshtoken");

        var narrowed = await RefreshAsync(initial.RefreshToken, "api1");
        Assert.False(narrowed.IsError);
        Assert.Equal("api1", narrowed.Scope);
        Assert.Equivalent(new[] { "api1" }, new JwtSecurityTokenHandler().ReadJwtToken(narrowed.AccessToken).Claims
            .Where(c => c.Type == JwtClaimTypes.Scope).Select(c => c.Value), strict: true);

        var full = await RefreshAsync(narrowed.RefreshToken);
        Assert.False(full.IsError);
        Assert.Equivalent(new[] { "openid", "api1", "offline_access" }, full.Scope.Split(' '), strict: true);
    }

    [Fact]
    public async Task Refresh_with_scope_not_originally_granted_should_fail()
    {
        var initial = await RequestInitialTokenAsync("client.refreshtoken", "api1 offline_access");

        var widened = await RefreshAsync(initial.RefreshToken, "openid api1");

        Assert.True(widened.IsError);
        Assert.Equal(OidcConstants.TokenErrors.InvalidScope, widened.Error);
    }
}
