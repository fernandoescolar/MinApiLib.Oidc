// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// Derived from IdentityServer4 4.1.2 (https://github.com/IdentityServer/IdentityServer4); modified for MinApiLib.Oidc.

namespace MinApiLib.Oidc.Server.Features.DeviceFlow;

/// <summary>
/// Event for device authorization success
/// </summary>
/// <seealso cref="MinApiLib.Oidc.Server.Core.Events.Event" />
public class DeviceAuthorizationSuccessEvent : Event
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DeviceAuthorizationSuccessEvent"/> class.
    /// </summary>
    /// <param name="request">The request.</param>
    public DeviceAuthorizationSuccessEvent(DeviceAuthorizationRequestValidationResult request)
        : this()
    {
        ClientId = request.ValidatedRequest.Client?.ClientId;
        ClientName = request.ValidatedRequest.Client?.ClientName;
        Endpoint = OidcServerConstants.EndpointNames.DeviceAuthorization;
        Scopes = request.ValidatedRequest.ValidatedResources?.RawScopeValues.ToSpaceSeparatedString();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DeviceAuthorizationSuccessEvent"/> class.
    /// </summary>
    protected DeviceAuthorizationSuccessEvent()
        : base(EventCategories.DeviceFlow,
            "Device Authorization Success",
            EventTypes.Success,
            EventIds.DeviceAuthorizationSuccess)
    {
    }

    /// <summary>
    /// Gets or sets the client identifier.
    /// </summary>
    /// <value>
    /// The client identifier.
    /// </value>
    public string? ClientId { get; set; }

    /// <summary>
    /// Gets or sets the name of the client.
    /// </summary>
    /// <value>
    /// The name of the client.
    /// </value>
    public string? ClientName { get; set; }

    /// <summary>
    /// Gets or sets the endpoint.
    /// </summary>
    /// <value>
    /// The endpoint.
    /// </value>
    public string? Endpoint { get; set; }

    /// <summary>
    /// Gets or sets the scopes.
    /// </summary>
    /// <value>
    /// The scopes.
    /// </value>
    public string? Scopes { get; set; }
}
