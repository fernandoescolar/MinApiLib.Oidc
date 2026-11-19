using System.Text.Json.Nodes;

namespace IdP.Features.Api.Admin;

// Identity resources, API scopes and API resources (/api/admin/identity-resources, /api/admin/api-scopes,
// /api/admin/api-resources): the server's models as JSON, keyed by name.

public sealed record ListIdentityResources() : GetHandlerAsync<ListIdentityResources.Request>("/api/admin/identity-resources")
{
    public readonly record struct Request([FromServices] OidcServerMongoContext Db);

    protected override RouteHandlerBuilder Configure(RouteHandlerBuilder builder) => builder.RequireAdmin();

    protected override async Task<IResult> HandleAsync(Request request, CancellationToken cancellationToken)
        => Results.Json(await request.Db.IdentityResources.Find(FilterDefinition<IdentityResource>.Empty).SortBy(r => r.Name).ToListAsync(cancellationToken), AdminJson.Options);
}

public sealed record SaveIdentityResource() : PutHandlerAsync<SaveIdentityResource.Request>("/api/admin/identity-resources/{name}")
{
    public readonly record struct Request(HttpContext HttpContext, [FromRoute] string Name, [FromBody] JsonElement Body, [FromServices] OidcServerMongoContext Db, [FromServices] IEventService Events);

    protected override RouteHandlerBuilder Configure(RouteHandlerBuilder builder) => builder.RequireAdmin();

    protected override async Task<IResult> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var resource = AdminJson.Read<IdentityResource>(request.Body);
        if (resource == null || !Resources.IsValidName(request.Name))
        {
            return ApiErrors.Validation("Invalid identity resource.");
        }

        resource.Name = request.Name;
        var created = !await request.Db.IdentityResources.Find(r => r.Name == request.Name).AnyAsync(cancellationToken);
        await request.Db.UpsertAsync(resource, cancellationToken);
        await Audit.RaiseAsync(request.Events, request.HttpContext, created ? "Identity resource created" : "Identity resource updated", request.Name);
        return Results.Json(resource, AdminJson.Options, statusCode: created ? StatusCodes.Status201Created : StatusCodes.Status200OK);
    }
}

public sealed record DeleteIdentityResource() : DeleteHandlerAsync<DeleteIdentityResource.Request>("/api/admin/identity-resources/{name}")
{
    public readonly record struct Request(HttpContext HttpContext, [FromRoute] string Name, [FromServices] OidcServerMongoContext Db, [FromServices] IEventService Events);

    protected override RouteHandlerBuilder Configure(RouteHandlerBuilder builder) => builder.RequireAdmin();

    protected override async Task<IResult> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        if (request.Name == OidcConstants.StandardScopes.OpenId)
        {
            return ApiErrors.Validation("The openid scope is required by OpenID Connect.");
        }

        var result = await request.Db.IdentityResources.DeleteOneAsync(r => r.Name == request.Name, cancellationToken);
        if (result.DeletedCount == 0)
        {
            return Results.NotFound();
        }

        await Audit.RaiseAsync(request.Events, request.HttpContext, "Identity resource deleted", request.Name);
        return Results.NoContent();
    }
}

public sealed record ListApiScopes() : GetHandlerAsync<ListApiScopes.Request>("/api/admin/api-scopes")
{
    public readonly record struct Request([FromServices] OidcServerMongoContext Db);

    protected override RouteHandlerBuilder Configure(RouteHandlerBuilder builder) => builder.RequireAdmin();

    protected override async Task<IResult> HandleAsync(Request request, CancellationToken cancellationToken)
        => Results.Json(await request.Db.ApiScopes.Find(FilterDefinition<ApiScope>.Empty).SortBy(s => s.Name).ToListAsync(cancellationToken), AdminJson.Options);
}

public sealed record SaveApiScope() : PutHandlerAsync<SaveApiScope.Request>("/api/admin/api-scopes/{name}")
{
    public readonly record struct Request(HttpContext HttpContext, [FromRoute] string Name, [FromBody] JsonElement Body, [FromServices] OidcServerMongoContext Db, [FromServices] IEventService Events);

    protected override RouteHandlerBuilder Configure(RouteHandlerBuilder builder) => builder.RequireAdmin();

    protected override async Task<IResult> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var scope = AdminJson.Read<ApiScope>(request.Body);
        if (scope == null || !Resources.IsValidName(request.Name))
        {
            return ApiErrors.Validation("Invalid API scope.");
        }

        scope.Name = request.Name;
        var created = !await request.Db.ApiScopes.Find(s => s.Name == request.Name).AnyAsync(cancellationToken);
        await request.Db.UpsertAsync(scope, cancellationToken);
        await Audit.RaiseAsync(request.Events, request.HttpContext, created ? "API scope created" : "API scope updated", request.Name);
        return Results.Json(scope, AdminJson.Options, statusCode: created ? StatusCodes.Status201Created : StatusCodes.Status200OK);
    }
}

public sealed record DeleteApiScope() : DeleteHandlerAsync<DeleteApiScope.Request>("/api/admin/api-scopes/{name}")
{
    public readonly record struct Request(HttpContext HttpContext, [FromRoute] string Name, [FromServices] OidcServerMongoContext Db, [FromServices] IEventService Events);

    protected override RouteHandlerBuilder Configure(RouteHandlerBuilder builder) => builder.RequireAdmin();

    protected override async Task<IResult> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var result = await request.Db.ApiScopes.DeleteOneAsync(s => s.Name == request.Name, cancellationToken);
        if (result.DeletedCount == 0)
        {
            return Results.NotFound();
        }

        await Audit.RaiseAsync(request.Events, request.HttpContext, "API scope deleted", request.Name);
        return Results.NoContent();
    }
}

public sealed record ListApiResources() : GetHandlerAsync<ListApiResources.Request>("/api/admin/api-resources")
{
    public readonly record struct Request([FromServices] OidcServerMongoContext Db);

    protected override RouteHandlerBuilder Configure(RouteHandlerBuilder builder) => builder.RequireAdmin();

    protected override async Task<IResult> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var resources = await request.Db.ApiResources.Find(FilterDefinition<ApiResource>.Empty).SortBy(r => r.Name).ToListAsync(cancellationToken);
        return Results.Json(new JsonArray(resources.Select(r => (JsonNode)AdminJson.With(r, r.ApiSecrets)).ToArray()), AdminJson.Options);
    }
}

public sealed record SaveApiResource() : PutHandlerAsync<SaveApiResource.Request>("/api/admin/api-resources/{name}")
{
    public readonly record struct Request(HttpContext HttpContext, [FromRoute] string Name, [FromBody] JsonElement Body, [FromServices] OidcServerMongoContext Db, [FromServices] IEventService Events);

    protected override RouteHandlerBuilder Configure(RouteHandlerBuilder builder) => builder.RequireAdmin();

    protected override async Task<IResult> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var resource = AdminJson.Read<ApiResource>(request.Body);
        if (resource == null || !Resources.IsValidName(request.Name))
        {
            return ApiErrors.Validation("Invalid API resource.");
        }

        resource.Name = request.Name;
        var existing = await request.Db.ApiResources.Find(r => r.Name == request.Name).FirstOrDefaultAsync(cancellationToken);
        resource.ApiSecrets = existing?.ApiSecrets ?? new HashSet<Secret>();
        await request.Db.UpsertAsync(resource, cancellationToken);
        await Audit.RaiseAsync(request.Events, request.HttpContext, existing == null ? "API resource created" : "API resource updated", request.Name);
        return Results.Json(AdminJson.With(resource, resource.ApiSecrets), AdminJson.Options, statusCode: existing == null ? StatusCodes.Status201Created : StatusCodes.Status200OK);
    }
}

public sealed record DeleteApiResource() : DeleteHandlerAsync<DeleteApiResource.Request>("/api/admin/api-resources/{name}")
{
    public readonly record struct Request(HttpContext HttpContext, [FromRoute] string Name, [FromServices] OidcServerMongoContext Db, [FromServices] IEventService Events);

    protected override RouteHandlerBuilder Configure(RouteHandlerBuilder builder) => builder.RequireAdmin();

    protected override async Task<IResult> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var result = await request.Db.ApiResources.DeleteOneAsync(r => r.Name == request.Name, cancellationToken);
        if (result.DeletedCount == 0)
        {
            return Results.NotFound();
        }

        await Audit.RaiseAsync(request.Events, request.HttpContext, "API resource deleted", request.Name);
        return Results.NoContent();
    }
}

/// <summary>Adds a secret to an API resource (for introspection), shown once.</summary>
public sealed record AddApiResourceSecret() : PostHandlerAsync<AddApiResourceSecret.Request>("/api/admin/api-resources/{name}/secrets")
{
    public readonly record struct Request(HttpContext HttpContext, [FromRoute] string Name, [FromBody] NewSecretBody Body, [FromServices] OidcServerMongoContext Db, [FromServices] IEventService Events);

    protected override RouteHandlerBuilder Configure(RouteHandlerBuilder builder) => builder.RequireAdmin();

    protected override async Task<IResult> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var resource = await request.Db.ApiResources.Find(r => r.Name == request.Name).FirstOrDefaultAsync(cancellationToken);
        if (resource == null)
        {
            return Results.NotFound();
        }

        var (clear, stored) = string.IsNullOrEmpty(request.Body.Value)
            ? SecretModel.Generate(request.Body.Description, request.Body.Expiration)
            : (request.Body.Value, new Secret(request.Body.Value.Sha256(), request.Body.Description ?? $"Created {DateTime.UtcNow:yyyy-MM-dd}", request.Body.Expiration));
        resource.ApiSecrets.Add(stored);
        await request.Db.UpsertAsync(resource, cancellationToken);
        await Audit.RaiseAsync(request.Events, request.HttpContext, "API resource secret added", $"{request.Name}: {SecretModel.Fingerprint(stored)}");
        return Results.Ok(new { secret = SecretModel.From(stored), value = clear });
    }
}

public sealed record RemoveApiResourceSecret() : DeleteHandlerAsync<RemoveApiResourceSecret.Request>("/api/admin/api-resources/{name}/secrets/{id}")
{
    public readonly record struct Request(HttpContext HttpContext, [FromRoute] string Name, [FromRoute] string Id, [FromServices] OidcServerMongoContext Db, [FromServices] IEventService Events);

    protected override RouteHandlerBuilder Configure(RouteHandlerBuilder builder) => builder.RequireAdmin();

    protected override async Task<IResult> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var resource = await request.Db.ApiResources.Find(r => r.Name == request.Name).FirstOrDefaultAsync(cancellationToken);
        var secret = resource?.ApiSecrets.FirstOrDefault(s => SecretModel.Fingerprint(s) == request.Id);
        if (resource == null || secret == null)
        {
            return Results.NotFound();
        }

        resource.ApiSecrets.Remove(secret);
        await request.Db.UpsertAsync(resource, cancellationToken);
        await Audit.RaiseAsync(request.Events, request.HttpContext, "API resource secret removed", $"{request.Name}: {request.Id}");
        return Results.NoContent();
    }
}

public static class Resources
{
    public static bool IsValidName(string? name)
        => !string.IsNullOrWhiteSpace(name) && name.Length <= 200 && !name.Any(char.IsWhiteSpace);
}
