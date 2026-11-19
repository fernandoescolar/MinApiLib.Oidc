namespace IdP.Features.Flow;

/// <summary>
/// Asks the user to let a client access the requested resources. The authorize endpoint redirects here when the client
/// requires consent and the user has not given it yet, with the authorize request in <c>returnUrl</c>.
/// </summary>
public sealed record ConsentPage() : GetHandlerAsync<ConsentPage.Request>("/consent")
{
    public readonly record struct Request(
        HttpContext HttpContext,
        [FromQuery] string? ReturnUrl,
        [FromServices] IOidcServerInteractionService Interaction,
        [FromServices] IAntiforgery Antiforgery,
        [FromServices] IOptions<IdPOptions> Options,
        [FromServices] SpaPages Spa);

    protected override async Task<IResult> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var context = request.ReturnUrl == null ? null : await request.Interaction.GetAuthorizationContextAsync(request.ReturnUrl);
        if (context == null)
        {
            return Results.Redirect("/error");
        }

        if (request.HttpContext.User.Identity?.IsAuthenticated != true)
        {
            return FlowPages.RedirectToLogin(request.ReturnUrl);
        }

        return Render(request.HttpContext, request.Spa, request.Antiforgery, request.Options.Value, request.ReturnUrl!, context.Client, context.ValidatedResources, error: null);
    }

    public static IResult Render(HttpContext http, SpaPages spa, IAntiforgery antiforgery, IdPOptions options, string returnUrl, Client client, MinApiLib.Oidc.Server.Core.ResourceValidation.ResourceValidationResult resources, string? error, string? userCode = null)
        => spa.Render("consent", "Consent", new
        {
            returnUrl,
            userCode,
            client = new { name = client.ClientName ?? client.ClientId, uri = client.ClientUri, logoUri = client.LogoUri, description = client.Description },
            allowRemember = client.AllowRememberConsent,
            identityScopes = resources.Resources.IdentityResources.Select(r => Scope(r.Name, r.DisplayName, r.Description, r.Required, r.Emphasize)),
            apiScopes = resources.ParsedScopes
                .Select(p => (parsed: p, scope: resources.Resources.ApiScopes.FirstOrDefault(s => s.Name == p.ParsedName)))
                .Where(x => x.scope != null)
                .Select(x => Scope(x.parsed.RawValue, x.scope!.DisplayName ?? x.parsed.RawValue, x.scope.Description, x.scope.Required, x.scope.Emphasize)),
            offlineAccess = resources.Resources.OfflineAccess,
            userName = http.User.FindFirst(JwtClaimTypes.Name)?.Value ?? http.User.FindFirst(JwtClaimTypes.PreferredUserName)?.Value,
            error,
            antiforgery = AntiforgeryModel.For(http, antiforgery),
            product = FlowPages.Product(options)
        });

    private static object Scope(string value, string? displayName, string? description, bool required, bool emphasize)
        => new { value, displayName = displayName ?? value, description, required, emphasize };
}

/// <summary>Records the user's decision and returns to the authorize endpoint.</summary>
public sealed record ConsentForm() : PostHandlerAsync<ConsentForm.Request>("/consent")
{
    public readonly record struct Request(
        HttpContext HttpContext,
        [FromForm] string? ReturnUrl,
        [FromForm] string? Button,
        [FromForm] bool? Remember,
        IFormCollection Form,
        [FromServices] IOidcServerInteractionService Interaction,
        [FromServices] IEventService Events,
        [FromServices] IAntiforgery Antiforgery,
        [FromServices] IOptions<IdPOptions> Options,
        [FromServices] SpaPages Spa);

    protected override async Task<IResult> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var context = request.ReturnUrl == null ? null : await request.Interaction.GetAuthorizationContextAsync(request.ReturnUrl);
        if (context == null)
        {
            return Results.Redirect("/error");
        }

        if (request.HttpContext.User.Identity?.IsAuthenticated != true)
        {
            return FlowPages.RedirectToLogin(request.ReturnUrl);
        }

        if (request.Button != "allow")
        {
            await request.Interaction.DenyAuthorizationAsync(context, AuthorizationError.AccessDenied);
            return Results.Redirect(request.ReturnUrl!);
        }

        var consent = Consent.Build(context.ValidatedResources, request.Form["scopes"].OfType<string>().ToArray(), request.Remember == true);
        if (consent == null)
        {
            return ConsentPage.Render(request.HttpContext, request.Spa, request.Antiforgery, request.Options.Value, request.ReturnUrl!, context.Client, context.ValidatedResources, "Choose at least one permission, or deny the request.");
        }

        await request.Interaction.GrantConsentAsync(context, consent);
        return Results.Redirect(request.ReturnUrl!);
    }
}

public static class Consent
{
    /// <summary>The scopes the user ticked, plus the required ones and offline access when requested; null when nothing was granted.</summary>
    public static ConsentResponse? Build(MinApiLib.Oidc.Server.Core.ResourceValidation.ResourceValidationResult resources, string[] chosen, bool remember)
    {
        var scopes = new HashSet<string>(chosen, StringComparer.Ordinal);
        foreach (var required in resources.Resources.IdentityResources.Where(r => r.Required).Select(r => r.Name))
        {
            scopes.Add(required);
        }

        foreach (var parsed in resources.ParsedScopes)
        {
            if (resources.Resources.ApiScopes.FirstOrDefault(s => s.Name == parsed.ParsedName)?.Required == true)
            {
                scopes.Add(parsed.RawValue);
            }
        }

        if (resources.Resources.OfflineAccess)
        {
            scopes.Add(OidcConstants.StandardScopes.OfflineAccess);
        }

        // Only what was requested can be granted.
        scopes.IntersectWith(resources.RawScopeValues);
        return scopes.Count == 0 ? null : new ConsentResponse { ScopesValuesConsented = scopes, RememberConsent = remember };
    }
}
