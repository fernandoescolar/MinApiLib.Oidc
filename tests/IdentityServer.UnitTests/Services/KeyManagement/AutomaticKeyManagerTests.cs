using IdentityServer.UnitTests.Common;
using Microsoft.AspNetCore.DataProtection;

namespace IdentityServer.UnitTests.Services.KeyManagement;

public class AutomaticKeyManagerTests : IDisposable
{
    private static readonly DateTime Start = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private readonly IdentityServerOptions _options = TestIdentityServerOptions.Create();
    private readonly InMemorySigningKeyStore _store = new();
    private readonly StubClock _clock = new();
    private readonly IDataProtectionProvider _dataProtection = new EphemeralDataProtectionProvider();
    private DateTime _now = Start;
    private AutomaticKeyManager _subject;

    public AutomaticKeyManagerTests()
    {
        _clock.UtcNowFunc = () => _now;
        _options.KeyManagement.Enabled = true;
        _options.KeyManagement.RotationInterval = TimeSpan.FromDays(90);
        _options.KeyManagement.PropagationTime = TimeSpan.FromDays(14);
        _options.KeyManagement.RetentionDuration = TimeSpan.FromDays(14);
        _options.KeyManagement.KeyCacheDuration = TimeSpan.FromHours(1);
        _subject = CreateManager();
    }

    public void Dispose()
    {
        _subject.Dispose();
        GC.SuppressFinalize(this);
    }

    private AutomaticKeyManager CreateManager()
        => new(_options, _store, _dataProtection, _clock, TestLogger.Create<AutomaticKeyManager>());

    private async Task<string> SigningKeyIdAsync() => (await _subject.GetSigningCredentialsAsync()).Single().Key.KeyId;

    private async Task<string[]> ValidationKeyIdsAsync() => (await _subject.GetValidationKeysAsync()).Select(k => k.Key.KeyId).ToArray();

    [Fact]
    public async Task First_use_creates_a_key_and_signs_with_it_immediately()
    {
        var kid = await SigningKeyIdAsync();

        Assert.False(string.IsNullOrEmpty(kid));
        Assert.Single((await _store.LoadKeysAsync()), k => k.Id == kid && k.Algorithm == SecurityAlgorithms.RsaSha256);
        Assert.Equal([kid], await ValidationKeyIdsAsync());
    }

    [Fact]
    public async Task Next_key_is_announced_before_it_signs_and_takes_over_at_rotation()
    {
        var first = await SigningKeyIdAsync();

        // 76 days: the next key is created and published, but the first key keeps signing.
        _now = Start.AddDays(76);
        Assert.Equal(first, (await SigningKeyIdAsync()));
        var validationKeyIds = await ValidationKeyIdsAsync();
        Assert.Equal(2, validationKeyIds.Count());
        var announced = validationKeyIds.Single(k => k != first);

        // 90 days: the first key retires; the announced key, published for 14 days, signs.
        _now = Start.AddDays(90);
        Assert.Equal(announced, (await SigningKeyIdAsync()));
        // a retired key stays published for its retention period
        Assert.Contains(first, await ValidationKeyIdsAsync());

        // 104 days: the first key is past its retention and removed from the store.
        _now = Start.AddDays(104);
        Assert.DoesNotContain(first, (await ValidationKeyIdsAsync()));
        Assert.DoesNotContain((await _store.LoadKeysAsync()), k => k.Id == first);
    }

    [Fact]
    public async Task Keys_are_shared_through_the_store_and_protected_at_rest()
    {
        var kid = await SigningKeyIdAsync();
        using var other = CreateManager();

        Assert.Equal(kid, (await other.GetSigningCredentialsAsync()).Single().Key.KeyId);
        var stored = (await _store.LoadKeysAsync()).Single();
        Assert.True(stored.DataProtected);
        // the PKCS#8 key must not be stored in clear text
        Assert.DoesNotContain("MII", stored.Data);
    }

    [Fact]
    public async Task Key_protected_with_another_key_ring_is_skipped()
    {
        await SigningKeyIdAsync();
        using var other = new AutomaticKeyManager(_options, _store, new EphemeralDataProtectionProvider(), _clock, TestLogger.Create<AutomaticKeyManager>());

        var credentials = await other.GetSigningCredentialsAsync();

        // an unreadable key is ignored and a readable one is created
        Assert.Single(credentials);
        Assert.Equal(2, (await _store.LoadKeysAsync()).Count());
    }

    [Fact]
    public async Task One_active_key_per_configured_algorithm()
    {
        _options.KeyManagement.SigningAlgorithms = [SecurityAlgorithms.RsaSha256, SecurityAlgorithms.EcdsaSha256];

        var credentials = await _subject.GetSigningCredentialsAsync();

        Assert.Equal(new[] { SecurityAlgorithms.RsaSha256, SecurityAlgorithms.EcdsaSha256 }, credentials.Select(c => c.Algorithm));
        Assert.IsType<ECDsaSecurityKey>(credentials[1].Key);
    }

    [Fact]
    public async Task Disabled_manager_creates_nothing()
    {
        _options.KeyManagement.Enabled = false;

        Assert.Empty((await _subject.GetSigningCredentialsAsync()));
        Assert.Empty((await _subject.GetValidationKeysAsync()));
        Assert.Empty((await _store.LoadKeysAsync()));
    }

    [Fact]
    public async Task Key_material_service_prefers_automatic_keys_and_publishes_both()
    {
        var staticKey = new RsaSecurityKey(RSA.Create(2048)) { KeyId = "static" };
        var service = new DefaultKeyMaterialService(
            [new InMemoryValidationKeysStore([new SecurityKeyInfo { Key = staticKey, SigningAlgorithm = SecurityAlgorithms.RsaSha256 }])],
            [new InMemorySigningCredentialsStore(new SigningCredentials(staticKey, SecurityAlgorithms.RsaSha256))],
            _subject);

        var signing = await service.GetSigningCredentialsAsync();

        Assert.Equal(await SigningKeyIdAsync(), signing.Key.KeyId);
        var validationKeyIds = (await service.GetValidationKeysAsync()).Select(k => k.Key.KeyId).ToList();
        Assert.Contains("static", validationKeyIds);
        Assert.Contains(signing.Key.KeyId, validationKeyIds);
        var unsupported = () => service.GetSigningCredentialsAsync([SecurityAlgorithms.EcdsaSha256]);
        await Assert.ThrowsAnyAsync<InvalidOperationException>(unsupported);
    }

    [Fact]
    public async Task File_system_store_round_trips_keys_and_rejects_unsafe_ids()
    {
        var path = Path.Combine(Path.GetTempPath(), "is-keys-" + Guid.NewGuid().ToString("N"));
        try
        {
            var store = new FileSystemSigningKeyStore(path, TestLogger.Create<FileSystemSigningKeyStore>());
            await store.StoreKeyAsync(new SerializedKey { Id = "abc123", Created = Start, Algorithm = "RS256", Data = "x" });

            Assert.Single((await store.LoadKeysAsync()), k => k.Id == "abc123" && k.Created == Start);

            await store.DeleteKeyAsync("abc123");
            Assert.Empty((await store.LoadKeysAsync()));

            var act = () => store.DeleteKeyAsync("../../etc/passwd");
            await Assert.ThrowsAnyAsync<ArgumentException>(act);
        }
        finally
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }
        }
    }
}
