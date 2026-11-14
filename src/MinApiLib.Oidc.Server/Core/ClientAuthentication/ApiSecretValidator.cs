// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// Derived from IdentityServer4 4.1.2 (https://github.com/IdentityServer/IdentityServer4); modified for MinApiLib.Oidc.

namespace MinApiLib.Oidc.Server.Core.ClientAuthentication;

/// <summary>
/// Validates API secrets using the registered secret validators and parsers
/// </summary>
[RegisterInIServiceCollection(typeof(IApiSecretValidator))]
public class ApiSecretValidator : IApiSecretValidator
{
    private readonly ILogger _logger;
    private readonly IResourceStore _resources;
    private readonly IEventService _events;
    private readonly ISecretsListParser _parser;
    private readonly ISecretsListValidator _validator;

    /// <summary>
    /// Initializes a new instance of the <see cref="ApiSecretValidator"/> class.
    /// </summary>
    /// <param name="resources">The resources.</param>
    /// <param name="parsers">The parsers.</param>
    /// <param name="validator">The validator.</param>
    /// <param name="events">The events.</param>
    /// <param name="logger">The logger.</param>
    public ApiSecretValidator(IResourceStore resources, ISecretsListParser parsers, ISecretsListValidator validator, IEventService events, ILogger<ApiSecretValidator> logger)
    {
        _resources = resources;
        _parser = parsers;
        _validator = validator;
        _events = events;
        _logger = logger;
    }

    /// <summary>
    /// Validates the secret on the current request.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <returns></returns>
    public async Task<ApiSecretValidationResult> ValidateAsync(HttpContext context)
    {
        SecretLog.StartApiValidation(_logger);

        var fail = new ApiSecretValidationResult
        {
            IsError = true
        };

        var parsedSecret = await _parser.ParseAsync(context);
        if (parsedSecret == null)
        {
            await RaiseFailureEventAsync("unknown", "No API id or secret found");

            SecretLog.NoApiSecretFound(_logger);
            return fail;
        }

        var apis = await _resources.FindApiResourcesByNameAsync(new[] { parsedSecret.Id });
        if (apis == null || !apis.Any())
        {
            await RaiseFailureEventAsync(parsedSecret.Id, "Unknown API resource");

            SecretLog.ApiResourceNotFound(_logger);
            return fail;
        }

        if (apis.Count() > 1)
        {
            await RaiseFailureEventAsync(parsedSecret.Id, "Invalid API resource");

            SecretLog.MultipleApiResourcesFound(_logger);
            return fail;
        }

        var api = apis.Single();

        if (api.Enabled == false)
        {
            await RaiseFailureEventAsync(parsedSecret.Id, "API resource not enabled");

            SecretLog.ApiResourceNotEnabled(_logger);
            return fail;
        }

        var result = await _validator.ValidateAsync(api.ApiSecrets, parsedSecret);
        if (result.Success)
        {
            SecretLog.ApiResourceValidationSuccess(_logger);

            var success = new ApiSecretValidationResult
            {
                IsError = false,
                Resource = api
            };

            await RaiseSuccessEventAsync(api.Name, parsedSecret.Type);
            return success;
        }

        await RaiseFailureEventAsync(api.Name, "Invalid API secret");
        SecretLog.ApiValidationFailed(_logger);

        return fail;
    }

    private Task RaiseSuccessEventAsync(string clientId, string authMethod)
    {
        return _events.RaiseAsync(new ApiAuthenticationSuccessEvent(clientId, authMethod));
    }

    private Task RaiseFailureEventAsync(string clientId, string message)
    {
        return _events.RaiseAsync(new ApiAuthenticationFailureEvent(clientId, message));
    }
}
