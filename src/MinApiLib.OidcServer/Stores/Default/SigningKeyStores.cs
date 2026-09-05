namespace MinApiLib.OidcServer.Stores;

/// <summary>
/// Stores signing keys as JSON files in a directory. Suitable for a single instance or a shared volume.
/// </summary>
[RegisterInIServiceCollection(typeof(ISigningKeyStore), ServiceLifetime.Singleton)]
public class FileSystemSigningKeyStore : ISigningKeyStore
{
    private const string FilePrefix = "is-signing-key-";
    private const string FileExtension = ".json";

    private readonly DirectoryInfo _directory;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileSystemSigningKeyStore"/> class.
    /// </summary>
    public FileSystemSigningKeyStore(OidcServerOptions options, ILogger<FileSystemSigningKeyStore> logger)
        : this(options.KeyManagement.KeyPath, logger)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FileSystemSigningKeyStore"/> class.
    /// </summary>
    public FileSystemSigningKeyStore(string path, ILogger<FileSystemSigningKeyStore> logger)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        _directory = new DirectoryInfo(path);
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<SerializedKey>> LoadKeysAsync()
    {
        var keys = new List<SerializedKey>();
        if (!_directory.Exists)
        {
            return keys;
        }

        foreach (var file in _directory.EnumerateFiles(FilePrefix + "*" + FileExtension))
        {
            try
            {
                await using var stream = file.OpenRead();
                var key = await JsonSerializer.DeserializeAsync<SerializedKey>(stream);
                if (key?.Id != null)
                {
                    keys.Add(key);
                }
            }
            catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
            {
                SecretLog.SigningKeyFileReadFailed(_logger, ex, file.FullName);
            }
        }

        return keys;
    }

    /// <inheritdoc/>
    public async Task StoreKeyAsync(SerializedKey key)
    {
        ArgumentNullException.ThrowIfNull(key);

        _directory.Create();
        var path = GetPath(key.Id);
        var temp = path + ".tmp";
        await File.WriteAllTextAsync(temp, JsonSerializer.Serialize(key));
        File.Move(temp, path, overwrite: true);
    }

    /// <inheritdoc/>
    public Task DeleteKeyAsync(string id)
    {
        var path = GetPath(id);
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        return Task.CompletedTask;
    }

    private string GetPath(string id)
    {
        // Key ids are generated hex strings; anything else cannot be a file this store wrote.
        if (string.IsNullOrEmpty(id) || !id.All(Uri.IsHexDigit))
        {
            throw new ArgumentException("Invalid key id", nameof(id));
        }

        return Path.Combine(_directory.FullName, FilePrefix + id + FileExtension);
    }
}

/// <summary>
/// Keeps signing keys in memory. Keys are lost on restart, so use it only for tests and development.
/// </summary>
public class InMemorySigningKeyStore : ISigningKeyStore
{
    private readonly ConcurrentDictionary<string, SerializedKey> _keys = new();

    /// <inheritdoc/>
    public Task<IEnumerable<SerializedKey>> LoadKeysAsync() => Task.FromResult<IEnumerable<SerializedKey>>(_keys.Values.ToArray());

    /// <inheritdoc/>
    public Task StoreKeyAsync(SerializedKey key)
    {
        ArgumentNullException.ThrowIfNull(key);
        _keys[key.Id] = key;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task DeleteKeyAsync(string id)
    {
        _keys.TryRemove(id, out _);
        return Task.CompletedTask;
    }
}
