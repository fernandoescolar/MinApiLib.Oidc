// Derived from IdentityModel 7.0.0 (https://github.com/IdentityModel/IdentityModel), Copyright (c) Brock Allen & Dominick Baier,
// Apache-2.0: test-only protocol client.

#nullable enable

// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System.Threading.Tasks;
using IdentityServer.IntegrationTests.ProtocolClient.Jwk;

namespace IdentityServer.IntegrationTests.ProtocolClient;

/// <summary>
/// Models a response from a JWK endpoint
/// </summary>
/// <seealso cref="IdentityModel.Client.ProtocolResponse" />
public class JsonWebKeySetResponse : ProtocolResponse
{
    /// <summary>
    /// Initializes the key set
    /// </summary>
    /// <param name="initializationData"></param>
    /// <returns></returns>
    protected override Task InitializeAsync(object? initializationData = null)
    {
        if (HttpResponse?.IsSuccessStatusCode != true)
        {
            ErrorMessage = initializationData as string;
        }
        else
        {
            KeySet = new Jwk.JsonWebKeySet(Raw!);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// The key set
    /// </summary>
    public Jwk.JsonWebKeySet? KeySet { get; set; }
}