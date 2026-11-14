// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// Derived from IdentityServer4 4.1.2 (https://github.com/IdentityServer/IdentityServer4); modified for MinApiLib.Oidc.

namespace MinApiLib.Oidc.Server.Core.Configuration;

internal static class EndpointOptionsExtensions
{
    public static bool IsEndpointEnabled(this EndpointsOptions options, string name)
    {
        return name switch
        {
            EndpointNames.Authorize => options.EnableAuthorizeEndpoint,
            EndpointNames.CheckSession => options.EnableCheckSessionEndpoint,
            EndpointNames.DeviceAuthorization => options.EnableDeviceAuthorizationEndpoint,
            EndpointNames.Discovery => options.EnableDiscoveryEndpoint,
            EndpointNames.EndSession => options.EnableEndSessionEndpoint,
            EndpointNames.Introspection => options.EnableIntrospectionEndpoint,
            EndpointNames.Revocation => options.EnableTokenRevocationEndpoint,
            EndpointNames.Token => options.EnableTokenEndpoint,
            EndpointNames.UserInfo => options.EnableUserInfoEndpoint,
            EndpointNames.Nonce => options.EnableNonceEndpoint,
            EndpointNames.PushedAuthorization => options.EnablePushedAuthorizationEndpoint,
            _ => true
        };
    }
}
