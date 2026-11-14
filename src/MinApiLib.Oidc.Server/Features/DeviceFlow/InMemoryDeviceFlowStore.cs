// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// Derived from IdentityServer4 4.1.2 (https://github.com/IdentityServer/IdentityServer4); modified for MinApiLib.Oidc.

namespace MinApiLib.Oidc.Server.Features.DeviceFlow;

/// <summary>
/// In-memory device flow store. Storing a device authorization also removes the expired ones, at most once a minute.
/// </summary>
/// <seealso cref="MinApiLib.Oidc.Server.Features.DeviceFlow.IDeviceFlowStore" />
public class InMemoryDeviceFlowStore : IDeviceFlowStore
{
    private static readonly TimeSpan PurgeInterval = TimeSpan.FromMinutes(1);

    private readonly List<InMemoryDeviceAuthorization> _repository = new List<InMemoryDeviceAuthorization>();
    private readonly TimeProvider _clock;
    private DateTimeOffset _nextPurge;

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryDeviceFlowStore"/> class with the system clock.
    /// </summary>
    public InMemoryDeviceFlowStore()
        : this(TimeProvider.System)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryDeviceFlowStore"/> class.
    /// </summary>
    /// <param name="clock">The clock that decides which device authorizations have expired.</param>
    public InMemoryDeviceFlowStore(TimeProvider clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        _clock = clock;
    }

    /// <summary>
    /// Stores the device authorization request.
    /// </summary>
    /// <param name="deviceCode">The device code.</param>
    /// <param name="userCode">The user code.</param>
    /// <param name="data">The data.</param>
    /// <returns></returns>
    public Task StoreDeviceAuthorizationAsync(string deviceCode, string userCode, DeviceCode data)
    {
        lock (_repository)
        {
            var now = _clock.GetUtcNow();
            if (now >= _nextPurge)
            {
                _nextPurge = now.Add(PurgeInterval);
                _repository.RemoveAll(x => x.Data.CreationTime.AddSeconds(x.Data.Lifetime) < now.UtcDateTime);
            }

            _repository.Add(new InMemoryDeviceAuthorization(deviceCode, userCode, data));
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Finds device authorization by user code.
    /// </summary>
    /// <param name="userCode">The user code.</param>
    public Task<DeviceCode?> FindByUserCodeAsync(string userCode)
    {
        DeviceCode? foundDeviceCode;

        lock (_repository)
        {
            foundDeviceCode = _repository.FirstOrDefault(x => x.UserCode == userCode)?.Data;
        }

        return Task.FromResult(foundDeviceCode);
    }

    /// <summary>
    /// Finds device authorization by device code.
    /// </summary>
    /// <param name="deviceCode">The device code.</param>
    public Task<DeviceCode?> FindByDeviceCodeAsync(string deviceCode)
    {
        DeviceCode? foundDeviceCode;

        lock (_repository)
        {
            foundDeviceCode = _repository.FirstOrDefault(x => x.DeviceCode == deviceCode)?.Data;
        }

        return Task.FromResult(foundDeviceCode);
    }

    /// <summary>
    /// Updates device authorization, searching by user code.
    /// </summary>
    /// <param name="userCode">The user code.</param>
    /// <param name="data">The data.</param>
    public Task UpdateByUserCodeAsync(string userCode, DeviceCode data)
    {
        lock (_repository)
        {
            var foundData = _repository.FirstOrDefault(x => x.UserCode == userCode);

            if (foundData != null)
            {
                foundData.Data = data;
            }
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Removes the device authorization, searching by device code.
    /// </summary>
    /// <param name="deviceCode">The device code.</param>
    /// <returns></returns>
    public Task RemoveByDeviceCodeAsync(string deviceCode)
    {
        lock (_repository)
        {
            var foundData = _repository.FirstOrDefault(x => x.DeviceCode == deviceCode);

            if (foundData != null)
            {
                _repository.Remove(foundData);
            }
        }

        return Task.CompletedTask;
    }

    private class InMemoryDeviceAuthorization
    {
        public InMemoryDeviceAuthorization(string deviceCode, string userCode, DeviceCode data)
        {
            DeviceCode = deviceCode;
            UserCode = userCode;
            Data = data;
        }

        public string DeviceCode { get; }
        public string UserCode { get; }
        public DeviceCode Data { get; set; }
    }
}
