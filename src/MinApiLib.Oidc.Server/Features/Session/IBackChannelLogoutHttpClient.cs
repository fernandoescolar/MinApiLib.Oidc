// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// Derived from IdentityServer4 4.1.2 (https://github.com/IdentityServer/IdentityServer4); modified for MinApiLib.Oidc.

namespace MinApiLib.Oidc.Server.Features.Session;

/// <summary>
/// Models making HTTP requests for back-channel logout notification.
/// </summary>
public interface IBackChannelLogoutHttpClient
{
    /// <summary>
    /// Performs HTTP POST.
    /// </summary>
    /// <param name="url"></param>
    /// <param name="payload"></param>
    /// <returns></returns>
    Task PostAsync(string url, Dictionary<string, string> payload);
}
