// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// Derived from IdentityServer4 4.1.2 (https://github.com/IdentityServer/IdentityServer4); modified for MinApiLib.Oidc.

namespace MinApiLib.Oidc.Server.Features.Authorize;

/// <summary>
/// Result for consent page
/// </summary>
/// <seealso cref="MinApiLib.Oidc.Server.Core.Hosting.IEndpointResult" />
public class ConsentPageResult : IEndpointResult
{
    private readonly ValidatedAuthorizeRequest _request;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsentPageResult"/> class.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <exception cref="System.ArgumentNullException">request</exception>
    public ConsentPageResult(ValidatedAuthorizeRequest request)
    {
        _request = request ?? throw new ArgumentNullException(nameof(request));
    }

    internal ConsentPageResult(
        ValidatedAuthorizeRequest request,
        OidcServerOptions options,
        IAuthorizationParametersMessageStore? authorizationParametersMessageStore = null)
        : this(request)
    {
        _options = options;
        _authorizationParametersMessageStore = authorizationParametersMessageStore;
    }

    private OidcServerOptions _options = null!;
    private IAuthorizationParametersMessageStore? _authorizationParametersMessageStore;

    private TimeProvider _clock = null!;

    private void Init(HttpContext context)
    {
        _clock = _clock ?? context.RequestServices?.GetService<TimeProvider>() ?? TimeProvider.System;
        _options = _options ?? context.RequestServices!.GetRequiredService<OidcServerOptions>();
        _authorizationParametersMessageStore = _authorizationParametersMessageStore ?? context.RequestServices!.GetService<IAuthorizationParametersMessageStore>();
    }

    /// <summary>
    /// Executes the result.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns></returns>
    public async Task ExecuteAsync(HttpContext context)
    {
        Init(context);

        var returnUrl = context.GetOidcServerBasePath().EnsureTrailingSlash() + Constants.ProtocolRoutePaths.AuthorizeCallback;
        var pushedReturnUrl = await PushedAuthorizationReturnUrl.TryCreateAsync(context, _request, returnUrl);
        if (pushedReturnUrl != null)
        {
            returnUrl = pushedReturnUrl;
        }
        else if (_authorizationParametersMessageStore != null)
        {
            var msg = new Message<IDictionary<string, string[]>>(_request.Raw.ToFullDictionary(), _clock.GetUtcNow().UtcDateTime);
            var id = await _authorizationParametersMessageStore.WriteAsync(msg);
            returnUrl = returnUrl.AddQueryString(Constants.AuthorizationParamsStore.MessageStoreIdParameterName, id);
        }
        else
        {
            returnUrl = returnUrl.AddQueryString(_request.Raw.ToQueryString());
        }

        var consentUrl = _options.UserInteraction.ConsentUrl;
        if (!consentUrl.IsLocalUrl())
        {
            returnUrl = context.GetOidcServerHost().EnsureTrailingSlash() + returnUrl.RemoveLeadingSlash();
        }

        var url = consentUrl.AddQueryString(_options.UserInteraction.ConsentReturnUrlParameter, returnUrl);
        context.Response.RedirectToAbsoluteUrl(url);
    }
}
