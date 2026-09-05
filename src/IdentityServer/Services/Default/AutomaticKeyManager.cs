namespace IdentityServer.Services;

/// <summary>
/// Creates, rotates and retires signing keys. A key is published as soon as it exists, signs tokens once it is
/// older than <see cref="KeyManagementOptions.PropagationTime"/>, retires after
/// <see cref="KeyManagementOptions.RotationInterval"/> and is removed after <see cref="KeyManagementOptions.RetentionDuration"/>.
/// </summary>
internal sealed class AutomaticKeyManager : ISigningKeyManager, IDisposable
{
    private const string ProtectorPurpose = "IdentityServer.KeyManagement";

    private readonly KeyManagementOptions _options;
    private readonly ISigningKeyStore _store;
    private readonly IDataProtector _protector;
    private readonly TimeProvider _clock;
    private readonly ILogger _logger;
    private readonly SemaphoreSlim _lock = new(1, 1);

    private List<LoadedKey>? _cache;
    private DateTimeOffset _cacheExpiration;

    private sealed record LoadedKey(SerializedKey Stored, AsymmetricSecurityKey Key);

    public AutomaticKeyManager(IdentityServerOptions options, ISigningKeyStore store, IDataProtectionProvider dataProtectionProvider, TimeProvider clock, ILogger<AutomaticKeyManager> logger)
    {
        _options = options.KeyManagement;
        _store = store;
        _protector = dataProtectionProvider.CreateProtector(ProtectorPurpose);
        _clock = clock;
        _logger = logger;
    }

    public bool IsEnabled => _options.Enabled;

    /// <summary>
    /// The credential that currently signs for each configured algorithm, in configuration order.
    /// </summary>
    public async Task<IReadOnlyList<SigningCredentials>> GetSigningCredentialsAsync()
    {
        if (!IsEnabled)
        {
            return [];
        }

        var keys = await GetKeysAsync();
        var now = _clock.GetUtcNow().UtcDateTime;
        var credentials = new List<SigningCredentials>();

        foreach (var algorithm in _options.SigningAlgorithms)
        {
            var active = SelectActiveKey(keys.Where(k => k.Stored.Algorithm == algorithm), now);
            if (active != null)
            {
                credentials.Add(new SigningCredentials(active.Key, algorithm));
            }
        }

        return credentials;
    }

    /// <summary>
    /// Every key that is announced, active or retired but still retained.
    /// </summary>
    public async Task<IReadOnlyList<SecurityKeyInfo>> GetValidationKeysAsync()
    {
        if (!IsEnabled)
        {
            return [];
        }

        var keys = await GetKeysAsync();
        return keys.Select(k => new SecurityKeyInfo { Key = k.Key, SigningAlgorithm = k.Stored.Algorithm }).ToList();
    }

    private LoadedKey? SelectActiveKey(IEnumerable<LoadedKey> keys, DateTime now)
    {
        var current = keys.Where(k => Age(k, now) < _options.RotationInterval).OrderByDescending(k => k.Stored.Created).ToList();

        // Prefer the newest key that has been published long enough; at first start there is only an unpropagated one.
        return current.FirstOrDefault(k => Age(k, now) >= _options.PropagationTime) ?? current.LastOrDefault();
    }

    private static TimeSpan Age(LoadedKey key, DateTime now) => now - key.Stored.Created;

    private async Task<List<LoadedKey>> GetKeysAsync()
    {
        var now = _clock.GetUtcNow();
        var cache = _cache;
        if (cache != null && now < _cacheExpiration && !NeedsMaintenance(cache, now.UtcDateTime))
        {
            return cache;
        }

        await _lock.WaitAsync();
        try
        {
            now = _clock.GetUtcNow();
            if (_cache != null && now < _cacheExpiration && !NeedsMaintenance(_cache, now.UtcDateTime))
            {
                return _cache;
            }

            var keys = await LoadAndMaintainAsync(now.UtcDateTime);
            _cache = keys;
            _cacheExpiration = now + _options.KeyCacheDuration;
            return keys;
        }
        finally
        {
            _lock.Release();
        }
    }

    // A cached set needs maintenance when a key has expired or an algorithm needs its next key.
    private bool NeedsMaintenance(List<LoadedKey> keys, DateTime now)
        => keys.Any(k => Age(k, now) >= _options.RotationInterval + _options.RetentionDuration) ||
           _options.SigningAlgorithms.Any(algorithm => NeedsNewKey(keys.Where(k => k.Stored.Algorithm == algorithm), now));

    private bool NeedsNewKey(IEnumerable<LoadedKey> keys, DateTime now)
    {
        var newest = keys.OrderByDescending(k => k.Stored.Created).FirstOrDefault();
        return newest == null || Age(newest, now) >= _options.RotationInterval - _options.PropagationTime;
    }

    private async Task<List<LoadedKey>> LoadAndMaintainAsync(DateTime now)
    {
        var keys = new List<LoadedKey>();
        foreach (var stored in await _store.LoadKeysAsync())
        {
            var age = now - stored.Created;
            if (age >= _options.RotationInterval + _options.RetentionDuration)
            {
                if (_options.DeleteRetiredKeys)
                {
                    ServicesLog.DeletingRetiredSigningKey(_logger, stored.Id);
                    await _store.DeleteKeyAsync(stored.Id);
                }

                continue;
            }

            var key = TryLoad(stored);
            if (key != null)
            {
                keys.Add(new LoadedKey(stored, key));
            }
        }

        foreach (var algorithm in _options.SigningAlgorithms)
        {
            if (NeedsNewKey(keys.Where(k => k.Stored.Algorithm == algorithm), now))
            {
                keys.Add(await CreateKeyAsync(algorithm, now));
            }
        }

        return keys;
    }

    private async Task<LoadedKey> CreateKeyAsync(string algorithm, DateTime now)
    {
        var id = CryptoRandom.CreateUniqueId(16, CryptoRandom.OutputFormat.Hex);
        byte[] pkcs8;
        AsymmetricSecurityKey key;

        if (algorithm.StartsWith("ES", StringComparison.Ordinal))
        {
            var ecdsa = ECDsa.Create(algorithm switch
            {
                SecurityAlgorithms.EcdsaSha384 => ECCurve.NamedCurves.nistP384,
                SecurityAlgorithms.EcdsaSha512 => ECCurve.NamedCurves.nistP521,
                _ => ECCurve.NamedCurves.nistP256
            });
            pkcs8 = ecdsa.ExportPkcs8PrivateKey();
            key = new ECDsaSecurityKey(ecdsa) { KeyId = id };
        }
        else
        {
            var rsa = RSA.Create(2048);
            pkcs8 = rsa.ExportPkcs8PrivateKey();
            key = new RsaSecurityKey(rsa) { KeyId = id };
        }

        var data = Convert.ToBase64String(pkcs8);
        var stored = new SerializedKey
        {
            Id = id,
            Created = now,
            Algorithm = algorithm,
            Data = _options.DataProtectKeys ? _protector.Protect(data) : data,
            DataProtected = _options.DataProtectKeys
        };

        await _store.StoreKeyAsync(stored);
        ServicesLog.CreatedSigningKey(_logger, id, algorithm);

        return new LoadedKey(stored, key);
    }

    private AsymmetricSecurityKey? TryLoad(SerializedKey stored)
    {
        try
        {
            var data = stored.DataProtected ? _protector.Unprotect(stored.Data) : stored.Data;
            var pkcs8 = Convert.FromBase64String(data);

            if (stored.Algorithm.StartsWith("ES", StringComparison.Ordinal))
            {
                var ecdsa = ECDsa.Create();
                ecdsa.ImportPkcs8PrivateKey(pkcs8, out _);
                return new ECDsaSecurityKey(ecdsa) { KeyId = stored.Id };
            }

            var rsa = RSA.Create();
            rsa.ImportPkcs8PrivateKey(pkcs8, out _);
            return new RsaSecurityKey(rsa) { KeyId = stored.Id };
        }
        catch (Exception ex) when (ex is CryptographicException or FormatException)
        {
            // Typically a key protected by another data protection key ring: the instances do not share one.
            ServicesLog.SigningKeyLoadFailed(_logger, ex, stored.Id);
            return null;
        }
    }

    public void Dispose() => _lock.Dispose();
}
