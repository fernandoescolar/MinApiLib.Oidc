// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// Derived from IdentityServer4 4.1.2 (https://github.com/IdentityServer/IdentityServer4); modified for MinApiLib.Oidc.

namespace MinApiLib.Oidc.Server.Core.Clients;

/// <summary>
/// Default client configuration validator
/// </summary>
/// <seealso cref="MinApiLib.Oidc.Server.Core.Clients.IClientConfigurationValidator" />
[RegisterInIServiceCollection(typeof(IClientConfigurationValidator))]
public class DefaultClientConfigurationValidator : IClientConfigurationValidator
{
    private readonly OidcServerOptions _options;

    /// <summary>
    /// Constructor for DefaultClientConfigurationValidator
    /// </summary>
    public DefaultClientConfigurationValidator(OidcServerOptions options)
    {
        _options = options;
    }

    /// <summary>
    /// Determines whether the configuration of a client is valid.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <returns></returns>
    public async Task ValidateAsync(ClientConfigurationValidationContext context)
    {
        if (context.Client.ProtocolType == OidcServerConstants.ProtocolTypes.OpenIdConnect)
        {
            await ValidateGrantTypesAsync(context);
            if (context.IsValid == false) return;

            await ValidateLifetimesAsync(context);
            if (context.IsValid == false) return;

            await ValidateRedirectUriAsync(context);
            if (context.IsValid == false) return;

            await ValidateAllowedCorsOriginsAsync(context);
            if (context.IsValid == false) return;

            await ValidateUriSchemesAsync(context);
            if (context.IsValid == false) return;

            await ValidateSecretsAsync(context);
            if (context.IsValid == false) return;

            await ValidatePropertiesAsync(context);
            if (context.IsValid == false) return;
        }
    }

    /// <summary>
    /// Validates grant type related configuration settings.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <returns></returns>
    protected virtual Task ValidateGrantTypesAsync(ClientConfigurationValidationContext context)
    {
        if (!(context.Client.AllowedGrantTypes?.Count > 0))
        {
            context.SetError("no allowed grant type specified");
            return Task.CompletedTask;
        }

        if (context.Client.AllowedGrantTypes.Contains(GrantType.Implicit) && !context.Client.AllowImplicitFlow)
        {
            context.SetError("The implicit grant is deprecated: set AllowImplicitFlow to enable it for this client.");
            return Task.CompletedTask;
        }

        if (context.Client.AllowedGrantTypes.Contains(GrantType.ResourceOwnerPassword) && !context.Client.AllowResourceOwnerPasswordFlow)
        {
            context.SetError("The password grant is deprecated: set AllowResourceOwnerPasswordFlow to enable it for this client.");
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Validates lifetime related configuration settings.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <returns></returns>
    protected virtual Task ValidateLifetimesAsync(ClientConfigurationValidationContext context)
    {
        if (context.Client.AccessTokenLifetime <= 0)
        {
            context.SetError("access token lifetime is 0 or negative");
            return Task.CompletedTask;
        }

        if (context.Client.IdentityTokenLifetime <= 0)
        {
            context.SetError("identity token lifetime is 0 or negative");
            return Task.CompletedTask;
        }

        if (context.Client.AllowedGrantTypes?.Contains(GrantType.DeviceFlow) == true
            && context.Client.DeviceCodeLifetime <= 0)
        {
            context.SetError("device code lifetime is 0 or negative");
        }

        if (context.Client.AbsoluteRefreshTokenLifetime < 0)
        {
            context.SetError("absolute refresh token lifetime is negative");
            return Task.CompletedTask;
        }

        if (context.Client.SlidingRefreshTokenLifetime < 0)
        {
            context.SetError("sliding refresh token lifetime is negative");
            return Task.CompletedTask;
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Validates redirect URI related configuration.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <returns></returns>
    protected virtual Task ValidateRedirectUriAsync(ClientConfigurationValidationContext context)
    {
        if (context.Client.AllowedGrantTypes?.Count > 0)
        {
            if (context.Client.AllowedGrantTypes.Contains(GrantType.AuthorizationCode) ||
                context.Client.AllowedGrantTypes.Contains(GrantType.Hybrid) ||
                context.Client.AllowedGrantTypes.Contains(GrantType.Implicit))
            {
                if (context.Client.RedirectUris?.Count == 0)
                {
                    context.SetError("No redirect URI configured.");
                }
            }
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Validates allowed CORS origins for valid format.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <returns></returns>
    protected virtual Task ValidateAllowedCorsOriginsAsync(ClientConfigurationValidationContext context)
    {
        if (context.Client.AllowedCorsOrigins?.Count > 0)
        {
            foreach (var origin in context.Client.AllowedCorsOrigins)
            {
                var fail = true;

                if (!string.IsNullOrWhiteSpace(origin) && Uri.TryCreate(origin, UriKind.Absolute, out var uri))
                {
                    if (uri.AbsolutePath == "/" && !origin.EndsWith('/'))
                    {
                        fail = false;
                    }
                }

                if (fail)
                {
                    if (!string.IsNullOrWhiteSpace(origin))
                    {
                        context.SetError($"AllowedCorsOrigins contains invalid origin: {origin}");
                    }
                    else
                    {
                        context.SetError($"AllowedCorsOrigins contains invalid origin. There is an empty value.");
                    }
                    return Task.CompletedTask;
                }
            }
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Validates that URI schemes is not in the list of invalid URI scheme prefixes, as controlled by the ValidationOptions.
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    protected virtual Task ValidateUriSchemesAsync(ClientConfigurationValidationContext context)
    {
        if (context.Client.RedirectUris?.Count > 0)
        {
            foreach (var uri in context.Client.RedirectUris)
            {
                if (_options.Validation.InvalidRedirectUriPrefixes
                    .Any(scheme => uri?.StartsWith(scheme, StringComparison.OrdinalIgnoreCase) == true))
                {
                    context.SetError($"RedirectUri '{uri}' uses invalid scheme. If this scheme should be allowed, then configure it via ValidationOptions.");
                }
            }
        }

        if (context.Client.PostLogoutRedirectUris?.Count > 0)
        {
            foreach (var uri in context.Client.PostLogoutRedirectUris)
            {
                if (_options.Validation.InvalidRedirectUriPrefixes
                    .Any(scheme => uri?.StartsWith(scheme, StringComparison.OrdinalIgnoreCase) == true))
                {
                    context.SetError($"PostLogoutRedirectUri '{uri}' uses invalid scheme. If this scheme should be allowed, then configure it via ValidationOptions.");
                }
            }
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Validates secret related configuration.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <returns></returns>
    protected virtual Task ValidateSecretsAsync(ClientConfigurationValidationContext context)
    {
        if (context.Client.AllowedGrantTypes?.Contains(GrantType.ClientCredentials) == true && !context.Client.RequireClientSecret)
        {
            context.SetError("Client secret is required for client_credentials, but RequireClientSecret is false.");
            return Task.CompletedTask;
        }

        if (context.Client.AllowedGrantTypes?.Count > 0)
        {
            // The implicit flow never authenticates the client.
            foreach (var grantType in context.Client.AllowedGrantTypes.Where(g => g != GrantType.Implicit))
            {
                if (context.Client.RequireClientSecret && context.Client.ClientSecrets.Count == 0)
                {
                    context.SetError($"Client secret is required for {grantType}, but no client secret is configured.");
                    return Task.CompletedTask;
                }
            }
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Validates properties related configuration settings.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <returns></returns>
    protected virtual Task ValidatePropertiesAsync(ClientConfigurationValidationContext context)
    {
        return Task.CompletedTask;
    }
}
