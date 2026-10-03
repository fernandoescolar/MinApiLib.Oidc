namespace MinApiLib.Oidc.Server.MongoDB;

/// <summary>
/// Device flow store backed by the <c>deviceCodes</c> collection. The <see cref="DeviceCode"/>, with its subject, is
/// stored serialized by <see cref="IPersistentGrantSerializer"/>, like the persisted grants.
/// </summary>
public class MongoDeviceFlowStore : IDeviceFlowStore
{
    private readonly OidcServerMongoContext _context;
    private readonly IPersistentGrantSerializer _serializer;

    /// <summary>
    /// Initializes a new instance of the <see cref="MongoDeviceFlowStore"/> class.
    /// </summary>
    public MongoDeviceFlowStore(OidcServerMongoContext context, IPersistentGrantSerializer serializer)
    {
        _context = context;
        _serializer = serializer;
    }

    /// <inheritdoc />
    public Task StoreDeviceAuthorizationAsync(string deviceCode, string userCode, DeviceCode data)
    {
        ArgumentNullException.ThrowIfNull(deviceCode);
        ArgumentNullException.ThrowIfNull(userCode);
        ArgumentNullException.ThrowIfNull(data);

        return _context.DeviceCodes.InsertOneAsync(ToDocument(deviceCode, userCode, data));
    }

    /// <inheritdoc />
    public async Task<DeviceCode?> FindByUserCodeAsync(string userCode)
    {
        ArgumentNullException.ThrowIfNull(userCode);
        var document = await _context.DeviceCodes.Find(d => d.UserCode == userCode).FirstOrDefaultAsync();
        return ToModel(document);
    }

    /// <inheritdoc />
    public async Task<DeviceCode?> FindByDeviceCodeAsync(string deviceCode)
    {
        ArgumentNullException.ThrowIfNull(deviceCode);
        var document = await _context.DeviceCodes.Find(d => d.Id == deviceCode).FirstOrDefaultAsync();
        return ToModel(document);
    }

    /// <inheritdoc />
    public async Task UpdateByUserCodeAsync(string userCode, DeviceCode data)
    {
        ArgumentNullException.ThrowIfNull(userCode);
        ArgumentNullException.ThrowIfNull(data);

        var existing = await _context.DeviceCodes.Find(d => d.UserCode == userCode).FirstOrDefaultAsync()
            ?? throw new InvalidOperationException("No device code exists for this user code.");

        await _context.DeviceCodes.ReplaceOneAsync(d => d.Id == existing.Id, ToDocument(existing.Id, userCode, data));
    }

    /// <inheritdoc />
    public Task RemoveByDeviceCodeAsync(string deviceCode)
    {
        ArgumentNullException.ThrowIfNull(deviceCode);
        return _context.DeviceCodes.DeleteOneAsync(d => d.Id == deviceCode);
    }

    private DeviceFlowDocument ToDocument(string deviceCode, string userCode, DeviceCode data) => new()
    {
        Id = deviceCode,
        UserCode = userCode,
        SubjectId = data.Subject?.FindFirst(JwtClaimTypes.Subject)?.Value,
        SessionId = data.SessionId,
        ClientId = data.ClientId,
        Description = data.Description,
        CreationTime = data.CreationTime,
        Expiration = data.CreationTime.AddSeconds(data.Lifetime),
        Data = _serializer.Serialize(data)
    };

    private DeviceCode? ToModel(DeviceFlowDocument? document)
        => document == null ? null : _serializer.Deserialize<DeviceCode>(document.Data);
}
