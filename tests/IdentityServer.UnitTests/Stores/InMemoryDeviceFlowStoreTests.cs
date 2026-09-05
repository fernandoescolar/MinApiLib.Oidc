namespace IdentityServer.UnitTests.Stores;

public class InMemoryDeviceFlowStoreTests
{
    private InMemoryDeviceFlowStore _store = new InMemoryDeviceFlowStore();

    [Fact]
    public async Task StoreDeviceAuthorizationAsync_should_persist_data_by_user_code()
    {
        var deviceCode = Guid.NewGuid().ToString();
        var userCode = Guid.NewGuid().ToString();
        var data = new DeviceCode
        {
            ClientId = Guid.NewGuid().ToString(),
            CreationTime = DateTime.UtcNow,
            Lifetime = 300,
            IsAuthorized = false,
            IsOpenId = true,
            Subject = null,
            RequestedScopes = new[] { "scope1", "scope2" }
        };

        await _store.StoreDeviceAuthorizationAsync(deviceCode, userCode, data);
        var foundData = await _store.FindByUserCodeAsync(userCode);

        Assert.Equal(data.ClientId, foundData.ClientId);
        Assert.Equal(data.CreationTime, foundData.CreationTime);
        Assert.Equal(data.Lifetime, foundData.Lifetime);
        Assert.Equal(data.IsAuthorized, foundData.IsAuthorized);
        Assert.Equal(data.IsOpenId, foundData.IsOpenId);
        Assert.Equal(data.Subject, foundData.Subject);
        Assert.Equivalent(data.RequestedScopes, foundData.RequestedScopes, strict: true);
    }

    [Fact]
    public async Task StoreDeviceAuthorizationAsync_should_persist_data_by_device_code()
    {
        var deviceCode = Guid.NewGuid().ToString();
        var userCode = Guid.NewGuid().ToString();
        var data = new DeviceCode
        {
            ClientId = Guid.NewGuid().ToString(),
            CreationTime = DateTime.UtcNow,
            Lifetime = 300,
            IsAuthorized = false,
            IsOpenId = true,
            Subject = null,
            RequestedScopes = new[] { "scope1", "scope2" }
        };

        await _store.StoreDeviceAuthorizationAsync(deviceCode, userCode, data);
        var foundData = await _store.FindByDeviceCodeAsync(deviceCode);

        Assert.Equal(data.ClientId, foundData.ClientId);
        Assert.Equal(data.CreationTime, foundData.CreationTime);
        Assert.Equal(data.Lifetime, foundData.Lifetime);
        Assert.Equal(data.IsAuthorized, foundData.IsAuthorized);
        Assert.Equal(data.IsOpenId, foundData.IsOpenId);
        Assert.Equal(data.Subject, foundData.Subject);
        Assert.Equivalent(data.RequestedScopes, foundData.RequestedScopes, strict: true);
    }

    [Fact]
    public async Task UpdateByUserCodeAsync_should_update_data()
    {
        var deviceCode = Guid.NewGuid().ToString();
        var userCode = Guid.NewGuid().ToString();
        var initialData = new DeviceCode
        {
            ClientId = Guid.NewGuid().ToString(),
            CreationTime = DateTime.UtcNow,
            Lifetime = 300,
            IsAuthorized = false,
            IsOpenId = true,
            Subject = null,
            RequestedScopes = new[] { "scope1", "scope2" }
        };

        await _store.StoreDeviceAuthorizationAsync(deviceCode, userCode, initialData);

        var updatedData = new DeviceCode
        {
            ClientId = Guid.NewGuid().ToString(),
            CreationTime = initialData.CreationTime.AddHours(2),
            Lifetime = initialData.Lifetime + 600,
            IsAuthorized = !initialData.IsAuthorized,
            IsOpenId = !initialData.IsOpenId,
            Subject = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim> { new Claim("sub", "123") })),
            RequestedScopes = new[] { "api1", "api2" }
        };

        await _store.UpdateByUserCodeAsync(userCode, updatedData);

        var foundData = await _store.FindByUserCodeAsync(userCode);

        Assert.Equal(updatedData.ClientId, foundData.ClientId);
        Assert.Equal(updatedData.CreationTime, foundData.CreationTime);
        Assert.Equal(updatedData.Lifetime, foundData.Lifetime);
        Assert.Equal(updatedData.IsAuthorized, foundData.IsAuthorized);
        Assert.Equal(updatedData.IsOpenId, foundData.IsOpenId);
        Assert.Equivalent(updatedData.Subject, foundData.Subject, strict: true);
        Assert.Equivalent(updatedData.RequestedScopes, foundData.RequestedScopes, strict: true);
    }

    [Fact]
    public async Task RemoveByDeviceCodeAsync_should_update_data()
    {
        var deviceCode = Guid.NewGuid().ToString();
        var userCode = Guid.NewGuid().ToString();
        var data = new DeviceCode
        {
            ClientId = Guid.NewGuid().ToString(),
            CreationTime = DateTime.UtcNow,
            Lifetime = 300,
            IsAuthorized = false,
            IsOpenId = true,
            Subject = null,
            RequestedScopes = new[] { "scope1", "scope2" }
        };

        await _store.StoreDeviceAuthorizationAsync(deviceCode, userCode, data);
        await _store.RemoveByDeviceCodeAsync(deviceCode);
        var foundData = await _store.FindByUserCodeAsync(userCode);

        Assert.Null(foundData);
    }
}
