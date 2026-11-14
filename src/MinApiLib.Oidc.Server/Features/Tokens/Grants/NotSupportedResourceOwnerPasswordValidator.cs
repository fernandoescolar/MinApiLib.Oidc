// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// Derived from IdentityServer4 4.1.2 (https://github.com/IdentityServer/IdentityServer4); modified for MinApiLib.Oidc.

namespace MinApiLib.Oidc.Server.Features.Tokens.Grants;

/// <summary>
/// Default resource owner password validator: without an implementation the password grant is not supported.
/// </summary>
[RegisterInIServiceCollection(typeof(IResourceOwnerPasswordValidator))]
public class NotSupportedResourceOwnerPasswordValidator : IResourceOwnerPasswordValidator
{
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="NotSupportedResourceOwnerPasswordValidator"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public NotSupportedResourceOwnerPasswordValidator(ILogger<NotSupportedResourceOwnerPasswordValidator> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public Task ValidateAsync(ResourceOwnerPasswordValidationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        context.Result = new GrantValidationResult(TokenRequestErrors.UnsupportedGrantType);
        RequestValidationLog.ResourceOwnerPasswordNotSupported(_logger);

        return Task.CompletedTask;
    }
}
