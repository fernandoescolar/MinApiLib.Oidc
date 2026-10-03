// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// Derived from IdentityModel 7.0.0 (https://github.com/IdentityModel/IdentityModel): test-only protocol client.

#nullable enable

using MinApiLib.Oidc.Server.IntegrationTests.ProtocolClient.Internal;
using System.Linq;

namespace MinApiLib.Oidc.Server.IntegrationTests.ProtocolClient;

/// <summary>
/// Helper class for creating request URLs
/// </summary>
public class RequestUrl
{
    private readonly string _baseUrl;

    /// <summary>
    /// Initializes a new instance of the <see cref="RequestUrl"/> class.
    /// </summary>
    /// <param name="baseUrl">The authorize endpoint.</param>
    public RequestUrl(string baseUrl)
    {
        _baseUrl = baseUrl;
    }

    /// <summary>
    /// Creates URL based on key/value input pairs.
    /// </summary>
    /// <param name="parameters">The query string parameters.</param>
    /// <returns></returns>
    public string Create(Parameters parameters)
    {
        if (parameters == null || !parameters.Any())
        {
            return _baseUrl;
        }

        return Internal.QueryHelpers.AddQueryString(_baseUrl, parameters);
    }
}