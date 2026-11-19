namespace IdP.Features.Api.Admin;

// External identity providers (/api/admin/identity-providers): OpenID Connect providers users can sign in with.

public sealed record IdentityProviderModel(string Alias, string DisplayName, bool Enabled, string Authority, string ClientId, bool HasClientSecret, IReadOnlyList<string> Scopes,
    bool GetClaimsFromUserInfoEndpoint, bool UsePkce, bool AutoProvision, bool TrustEmail, bool ShowOnLoginPage, int Order, string CallbackPath, DateTime CreatedAt, DateTime UpdatedAt, long LinkedUsers)
{
    public static async Task<IdentityProviderModel> FromAsync(IdentityProviderDocument p, IdPMongoContext db, CancellationToken cancellationToken)
        => new(p.Alias, p.DisplayName, p.Enabled, p.Authority, p.ClientId, !string.IsNullOrEmpty(p.ClientSecret), p.Scopes, p.GetClaimsFromUserInfoEndpoint, p.UsePkce,
            p.AutoProvision, p.TrustEmail, p.ShowOnLoginPage, p.Order, p.CallbackPath, p.CreatedAt, p.UpdatedAt,
            await db.Users.CountDocumentsAsync(Builders<IdPUser>.Filter.ElemMatch(u => u.Logins, l => l.LoginProvider == p.Alias), cancellationToken: cancellationToken));
}

public sealed record IdentityProviderInput(string? Alias, string? DisplayName, bool? Enabled, string? Authority, string? ClientId, string? ClientSecret, List<string>? Scopes,
    bool? GetClaimsFromUserInfoEndpoint, bool? UsePkce, bool? AutoProvision, bool? TrustEmail, bool? ShowOnLoginPage, int? Order);

public sealed record ListIdentityProviders() : GetHandlerAsync<ListIdentityProviders.Request>("/api/admin/identity-providers")
{
    public readonly record struct Request([FromServices] IdPMongoContext Db);

    protected override RouteHandlerBuilder Configure(RouteHandlerBuilder builder) => builder.RequireAdmin();

    protected override async Task<IResult> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var providers = await request.Db.IdentityProviders.Find(FilterDefinition<IdentityProviderDocument>.Empty).SortBy(p => p.Order).ThenBy(p => p.DisplayName).ToListAsync(cancellationToken);
        var models = new List<IdentityProviderModel>();
        foreach (var provider in providers)
        {
            models.Add(await IdentityProviderModel.FromAsync(provider, request.Db, cancellationToken));
        }

        return Results.Ok(models);
    }
}

public sealed record GetIdentityProvider() : GetHandlerAsync<GetIdentityProvider.Request>("/api/admin/identity-providers/{alias}")
{
    public readonly record struct Request([FromRoute] string Alias, [FromServices] IdPMongoContext Db);

    protected override RouteHandlerBuilder Configure(RouteHandlerBuilder builder) => builder.RequireAdmin();

    protected override async Task<IResult> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var provider = await request.Db.IdentityProviders.Find(p => p.Alias == request.Alias).FirstOrDefaultAsync(cancellationToken);
        return provider == null ? Results.NotFound() : Results.Ok(await IdentityProviderModel.FromAsync(provider, request.Db, cancellationToken));
    }
}

public sealed record CreateIdentityProvider() : PostHandlerAsync<CreateIdentityProvider.Request>("/api/admin/identity-providers")
{
    public readonly record struct Request(HttpContext HttpContext, [FromBody] IdentityProviderInput Body, [FromServices] IdPMongoContext Db, [FromServices] DynamicSchemeManager Schemes, [FromServices] IEventService Events);

    protected override RouteHandlerBuilder Configure(RouteHandlerBuilder builder) => builder.RequireAdmin();

    protected override async Task<IResult> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var body = request.Body;
        if (!IdentityProviderDocument.IsValidAlias(body.Alias))
        {
            return ApiErrors.Validation("The alias may only have letters, digits, - and _.", "alias");
        }

        var provider = new IdentityProviderDocument { Alias = body.Alias! };
        var error = IdentityProviders.Apply(provider, body, request.Schemes);
        if (error != null)
        {
            return error;
        }

        try
        {
            await request.Db.IdentityProviders.InsertOneAsync(provider, cancellationToken: cancellationToken);
        }
        catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            return ApiErrors.Validation("A provider with that alias already exists.", "alias");
        }

        await request.Schemes.SyncAsync(cancellationToken);
        await Audit.RaiseAsync(request.Events, request.HttpContext, "Identity provider created", provider.Alias);
        return Results.Json(await IdentityProviderModel.FromAsync(provider, request.Db, cancellationToken), Json.Options, statusCode: StatusCodes.Status201Created);
    }
}

public sealed record UpdateIdentityProvider() : PutHandlerAsync<UpdateIdentityProvider.Request>("/api/admin/identity-providers/{alias}")
{
    public readonly record struct Request(HttpContext HttpContext, [FromRoute] string Alias, [FromBody] IdentityProviderInput Body, [FromServices] IdPMongoContext Db,
        [FromServices] DynamicSchemeManager Schemes, [FromServices] IEventService Events);

    protected override RouteHandlerBuilder Configure(RouteHandlerBuilder builder) => builder.RequireAdmin();

    protected override async Task<IResult> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var provider = await request.Db.IdentityProviders.Find(p => p.Alias == request.Alias).FirstOrDefaultAsync(cancellationToken);
        if (provider == null)
        {
            return Results.NotFound();
        }

        var error = IdentityProviders.Apply(provider, request.Body, request.Schemes);
        if (error != null)
        {
            return error;
        }

        await request.Db.IdentityProviders.ReplaceOneAsync(p => p.Alias == provider.Alias, provider, cancellationToken: cancellationToken);
        await request.Schemes.SyncAsync(cancellationToken);
        await Audit.RaiseAsync(request.Events, request.HttpContext, "Identity provider updated", provider.Alias);
        return Results.Ok(await IdentityProviderModel.FromAsync(provider, request.Db, cancellationToken));
    }
}

public sealed record DeleteIdentityProvider() : DeleteHandlerAsync<DeleteIdentityProvider.Request>("/api/admin/identity-providers/{alias}")
{
    public readonly record struct Request(HttpContext HttpContext, [FromRoute] string Alias, [FromServices] IdPMongoContext Db, [FromServices] DynamicSchemeManager Schemes, [FromServices] IEventService Events);

    protected override RouteHandlerBuilder Configure(RouteHandlerBuilder builder) => builder.RequireAdmin();

    protected override async Task<IResult> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var result = await request.Db.IdentityProviders.DeleteOneAsync(p => p.Alias == request.Alias, cancellationToken);
        if (result.DeletedCount == 0)
        {
            return Results.NotFound();
        }

        await request.Schemes.SyncAsync(cancellationToken);
        await Audit.RaiseAsync(request.Events, request.HttpContext, "Identity provider deleted", request.Alias);
        return Results.NoContent();
    }
}

public static class IdentityProviders
{
    /// <summary>Copies the input to the document; a null field keeps the current value, and an empty secret keeps the stored one.</summary>
    public static IResult? Apply(IdentityProviderDocument provider, IdentityProviderInput body, DynamicSchemeManager schemes)
    {
        if (body.DisplayName != null)
        {
            provider.DisplayName = body.DisplayName.Trim();
        }

        if (body.Authority != null)
        {
            provider.Authority = body.Authority.Trim().TrimEnd('/');
        }

        if (body.ClientId != null)
        {
            provider.ClientId = body.ClientId.Trim();
        }

        if (string.IsNullOrEmpty(provider.DisplayName) || string.IsNullOrEmpty(provider.ClientId))
        {
            return ApiErrors.Validation("displayName and clientId are required.");
        }

        if (!Uri.TryCreate(provider.Authority, UriKind.Absolute, out var authority) || authority.Scheme is not ("https" or "http"))
        {
            return ApiErrors.Validation("The authority must be an absolute http(s) URL.", "authority");
        }

        if (!string.IsNullOrEmpty(body.ClientSecret))
        {
            provider.ClientSecret = schemes.ProtectSecret(body.ClientSecret);
        }

        provider.Enabled = body.Enabled ?? provider.Enabled;
        provider.Scopes = body.Scopes?.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()).Distinct().ToList() ?? provider.Scopes;
        if (!provider.Scopes.Contains("openid"))
        {
            provider.Scopes.Insert(0, "openid");
        }

        provider.GetClaimsFromUserInfoEndpoint = body.GetClaimsFromUserInfoEndpoint ?? provider.GetClaimsFromUserInfoEndpoint;
        provider.UsePkce = body.UsePkce ?? provider.UsePkce;
        provider.AutoProvision = body.AutoProvision ?? provider.AutoProvision;
        provider.TrustEmail = body.TrustEmail ?? provider.TrustEmail;
        provider.ShowOnLoginPage = body.ShowOnLoginPage ?? provider.ShowOnLoginPage;
        provider.Order = body.Order ?? provider.Order;
        provider.UpdatedAt = DateTime.UtcNow;
        return null;
    }
}
