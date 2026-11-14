// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// Derived from IdentityServer4 4.1.2 (https://github.com/IdentityServer/IdentityServer4); modified for MinApiLib.Oidc.

namespace MinApiLib.Oidc.Server.Features.DeviceFlow;

internal class DeviceAuthorizationRequestValidationLog
{
    public string? ClientId { get; set; }
    public string? ClientName { get; set; }
    public string? Scopes { get; set; }

    public Dictionary<string, string> Raw { get; set; }

    private static readonly string[] SensitiveValuesFilter = {
            OidcConstants.TokenRequest.ClientSecret,
            OidcConstants.TokenRequest.ClientAssertion
        };

    public DeviceAuthorizationRequestValidationLog(ValidatedDeviceAuthorizationRequest request)
    {
        Raw = request.Raw.ToScrubbedDictionary(SensitiveValuesFilter);

        if (request.Client != null)
        {
            ClientId = request.Client.ClientId;
            ClientName = request.Client.ClientName;
        }

        if (request.RequestedScopes != null)
        {
            Scopes = request.RequestedScopes.ToSpaceSeparatedString();
        }
    }

    public override string ToString()
    {
        return LogSerializer.Serialize(this);
    }
}
