// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// Derived from IdentityServer4 4.1.2 (https://github.com/IdentityServer/IdentityServer4); modified for MinApiLib.Oidc.

namespace MinApiLib.Oidc.Server.Features.Introspection;

/// <summary>
/// The introspection response generator
/// </summary>
/// <seealso cref="MinApiLib.Oidc.Server.Features.Introspection.IIntrospectionResponseGenerator" />
[RegisterInIServiceCollection(typeof(IIntrospectionResponseGenerator))]
public class IntrospectionResponseGenerator : IIntrospectionResponseGenerator
{
    /// <summary>
    /// Gets the events.
    /// </summary>
    /// <value>
    /// The events.
    /// </value>
    protected readonly IEventService Events;

    /// <summary>
    /// The logger
    /// </summary>
    protected readonly ILogger Logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="IntrospectionResponseGenerator" /> class.
    /// </summary>
    /// <param name="events">The events.</param>
    /// <param name="logger">The logger.</param>
    public IntrospectionResponseGenerator(IEventService events, ILogger<IntrospectionResponseGenerator> logger)
    {
        Events = events;
        Logger = logger;
    }

    /// <summary>
    /// Processes the response.
    /// </summary>
    /// <param name="validationResult">The validation result.</param>
    /// <returns></returns>
    public virtual async Task<Dictionary<string, object>> ProcessAsync(IntrospectionRequestValidationResult validationResult)
    {
        ResponseAndHostingLog.CreatingIntrospectionResponse(Logger);

        var response = new Dictionary<string, object>
        {
            { "active", false }
        };

        if (validationResult.IsActive == false)
        {
            ResponseAndHostingLog.CreatingInactiveIntrospectionResponse(Logger);
            await Events.RaiseAsync(new TokenIntrospectionSuccessEvent(validationResult));

            return response;
        }

        if (await AreExpectedScopesPresentAsync(validationResult) == false)
        {
            return response;
        }

        ResponseAndHostingLog.CreatingActiveIntrospectionResponse(Logger);

        response = (validationResult.Claims ?? []).Where(c => c.Type != JwtClaimTypes.Scope).ToClaimsDictionary();

        response.Add("active", true);

        var allowedScopes = validationResult.Api.Scopes;
        var scopes = (validationResult.Claims ?? []).Where(c => c.Type == JwtClaimTypes.Scope).Select(x => x.Value);
        scopes = scopes.Where(x => allowedScopes.Contains(x));
        response.Add("scope", scopes.ToSpaceSeparatedString());

        await Events.RaiseAsync(new TokenIntrospectionSuccessEvent(validationResult));
        return response;
    }

    /// <summary>
    /// Checks if the API resource is allowed to introspect the scopes.
    /// </summary>
    /// <param name="validationResult">The validation result.</param>
    /// <returns></returns>
    protected virtual async Task<bool> AreExpectedScopesPresentAsync(IntrospectionRequestValidationResult validationResult)
    {
        var apiScopes = validationResult.Api.Scopes;
        var tokenScopes = (validationResult.Claims ?? []).Where(c => c.Type == JwtClaimTypes.Scope);

        var tokenScopesThatMatchApi = tokenScopes.Where(c => apiScopes.Contains(c.Value));

        var result = false;

        if (tokenScopesThatMatchApi.Any())
        {
            result = true;
        }
        else
        {
            ResponseAndHostingLog.ExpectedScopeMissing(Logger, apiScopes);
            await Events.RaiseAsync(new TokenIntrospectionFailureEvent(validationResult.Api.Name, "Expected scopes are missing", validationResult.Token, apiScopes, tokenScopes.Select(s => s.Value)));
        }

        return result;
    }
}
