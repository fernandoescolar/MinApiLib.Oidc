using System.Text.Json.Nodes;
using MinApiLib.Oidc.Server.Core.Clients;

namespace IdP.Features.Api.Admin;

// The clients (/api/admin/clients): the full Client model of the server, as JSON, plus secret management.

public sealed record ClientSummary(string ClientId, string? ClientName, string? Description, bool Enabled, IReadOnlyList<string> AllowedGrantTypes, bool RequireConsent, bool RequireDPoP, int Secrets, string? LogoUri);

public sealed record ListClients() : GetHandlerAsync<ListClients.Request>("/api/admin/clients")
{
    public readonly record struct Request([FromQuery] string? Q, [FromQuery] int? Page, [FromQuery] int? PageSize, [FromServices] OidcServerMongoContext Db);

    protected override RouteHandlerBuilder Configure(RouteHandlerBuilder builder) => builder.RequireAdmin();

    protected override async Task<IResult> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var filter = string.IsNullOrWhiteSpace(request.Q)
            ? FilterDefinition<Client>.Empty
            : Builders<Client>.Filter.Or(
                Builders<Client>.Filter.Regex(c => c.ClientId, new BsonRegularExpression(System.Text.RegularExpressions.Regex.Escape(request.Q), "i")),
                Builders<Client>.Filter.Regex(c => c.ClientName, new BsonRegularExpression(System.Text.RegularExpressions.Regex.Escape(request.Q), "i")));

        var total = await request.Db.Clients.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
        var clients = await request.Db.Clients.Find(filter).SortBy(c => c.ClientId).Skip(Paging.Skip(request.Page, request.PageSize)).Limit(Paging.Size(request.PageSize)).ToListAsync(cancellationToken);
        var items = clients.Select(c => new ClientSummary(c.ClientId, c.ClientName, c.Description, c.Enabled, c.AllowedGrantTypes.ToList(), c.RequireConsent, c.RequireDPoP, c.ClientSecrets.Count, c.LogoUri)).ToList();
        return Results.Ok(new Page<ClientSummary>(items, total, Paging.Number(request.Page), Paging.Size(request.PageSize)));
    }
}

public sealed record GetClient() : GetHandlerAsync<GetClient.Request>("/api/admin/clients/{clientId}")
{
    public readonly record struct Request([FromRoute] string ClientId, [FromServices] OidcServerMongoContext Db);

    protected override RouteHandlerBuilder Configure(RouteHandlerBuilder builder) => builder.RequireAdmin();

    protected override async Task<IResult> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var client = await request.Db.Clients.Find(c => c.ClientId == request.ClientId).FirstOrDefaultAsync(cancellationToken);
        return client == null ? Results.NotFound() : Results.Json(AdminJson.With(client, client.ClientSecrets), AdminJson.Options);
    }
}

public sealed record CreateClient() : PostHandlerAsync<CreateClient.Request>("/api/admin/clients")
{
    public readonly record struct Request(HttpContext HttpContext, [FromBody] JsonElement Body, [FromServices] OidcServerMongoContext Db,
        [FromServices] IClientConfigurationValidator Validator, [FromServices] IEventService Events);

    protected override RouteHandlerBuilder Configure(RouteHandlerBuilder builder) => builder.RequireAdmin();

    protected override async Task<IResult> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var (client, error) = ClientInput.Parse(request.Body);
        if (client == null)
        {
            return ApiErrors.Validation(error ?? "Invalid client.");
        }

        if (await request.Db.Clients.Find(c => c.ClientId == client.ClientId).AnyAsync(cancellationToken))
        {
            return ApiErrors.Validation("A client with that id already exists.", "clientId");
        }

        // A secret given at creation is stored hashed; the caller keeps the clear value.
        string? secret = null;
        if (client.RequireClientSecret && !client.AllowedGrantTypes.All(g => g == GrantType.Implicit))
        {
            var (clear, stored) = SecretModel.Generate("Initial secret", null);
            client.ClientSecrets.Add(stored);
            secret = clear;
        }

        var validation = await ClientInput.ValidateAsync(request.Validator, client);
        if (validation != null)
        {
            return ApiErrors.Validation(validation);
        }

        await request.Db.UpsertAsync(client, cancellationToken);
        await Audit.RaiseAsync(request.Events, request.HttpContext, "Client created", client.ClientId, clientId: client.ClientId);
        var node = AdminJson.With(client, client.ClientSecrets);
        node["clientSecret"] = secret;
        return Results.Json(node, AdminJson.Options, statusCode: StatusCodes.Status201Created);
    }
}

public sealed record UpdateClient() : PutHandlerAsync<UpdateClient.Request>("/api/admin/clients/{clientId}")
{
    public readonly record struct Request(HttpContext HttpContext, [FromRoute] string ClientId, [FromBody] JsonElement Body, [FromServices] OidcServerMongoContext Db,
        [FromServices] IClientConfigurationValidator Validator, [FromServices] IEventService Events);

    protected override RouteHandlerBuilder Configure(RouteHandlerBuilder builder) => builder.RequireAdmin();

    protected override async Task<IResult> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var existing = await request.Db.Clients.Find(c => c.ClientId == request.ClientId).FirstOrDefaultAsync(cancellationToken);
        if (existing == null)
        {
            return Results.NotFound();
        }

        var (client, error) = ClientInput.Parse(request.Body);
        if (client == null)
        {
            return ApiErrors.Validation(error ?? "Invalid client.");
        }

        client.ClientId = existing.ClientId;
        client.ClientSecrets = existing.ClientSecrets;

        var validation = await ClientInput.ValidateAsync(request.Validator, client);
        if (validation != null)
        {
            return ApiErrors.Validation(validation);
        }

        await request.Db.UpsertAsync(client, cancellationToken);
        await Audit.RaiseAsync(request.Events, request.HttpContext, "Client updated", client.ClientId, clientId: client.ClientId);
        return Results.Json(AdminJson.With(client, client.ClientSecrets), AdminJson.Options);
    }
}

public sealed record DeleteClient() : DeleteHandlerAsync<DeleteClient.Request>("/api/admin/clients/{clientId}")
{
    public readonly record struct Request(HttpContext HttpContext, [FromRoute] string ClientId, [FromServices] OidcServerMongoContext Db,
        [FromServices] IPersistedGrantStore Grants, [FromServices] IEventService Events);

    protected override RouteHandlerBuilder Configure(RouteHandlerBuilder builder) => builder.RequireAdmin();

    protected override async Task<IResult> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var result = await request.Db.Clients.DeleteOneAsync(c => c.ClientId == request.ClientId, cancellationToken);
        if (result.DeletedCount == 0)
        {
            return Results.NotFound();
        }

        await request.Grants.RemoveAllAsync(new PersistedGrantFilter { ClientId = request.ClientId });
        await Audit.RaiseAsync(request.Events, request.HttpContext, "Client deleted", request.ClientId, clientId: request.ClientId);
        return Results.NoContent();
    }
}

/// <summary>Adds a secret: a random one, shown once in the answer, or the value given, which is stored hashed.</summary>
public sealed record AddClientSecret() : PostHandlerAsync<AddClientSecret.Request>("/api/admin/clients/{clientId}/secrets")
{
    public readonly record struct Request(HttpContext HttpContext, [FromRoute] string ClientId, [FromBody] NewSecretBody Body, [FromServices] OidcServerMongoContext Db, [FromServices] IEventService Events);

    protected override RouteHandlerBuilder Configure(RouteHandlerBuilder builder) => builder.RequireAdmin();

    protected override async Task<IResult> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var client = await request.Db.Clients.Find(c => c.ClientId == request.ClientId).FirstOrDefaultAsync(cancellationToken);
        if (client == null)
        {
            return Results.NotFound();
        }

        string clear;
        Secret stored;
        if (string.IsNullOrEmpty(request.Body.Value))
        {
            (clear, stored) = SecretModel.Generate(request.Body.Description, request.Body.Expiration);
        }
        else
        {
            clear = request.Body.Value;
            stored = new Secret(clear.Sha256(), request.Body.Description ?? $"Created {DateTime.UtcNow:yyyy-MM-dd}", request.Body.Expiration);
        }

        client.ClientSecrets.Add(stored);
        await request.Db.UpsertAsync(client, cancellationToken);
        await Audit.RaiseAsync(request.Events, request.HttpContext, "Client secret added", $"{client.ClientId}: {SecretModel.Fingerprint(stored)}", clientId: client.ClientId);
        return Results.Ok(new { secret = SecretModel.From(stored), value = clear });
    }
}

public sealed record RemoveClientSecret() : DeleteHandlerAsync<RemoveClientSecret.Request>("/api/admin/clients/{clientId}/secrets/{id}")
{
    public readonly record struct Request(HttpContext HttpContext, [FromRoute] string ClientId, [FromRoute] string Id, [FromServices] OidcServerMongoContext Db, [FromServices] IEventService Events);

    protected override RouteHandlerBuilder Configure(RouteHandlerBuilder builder) => builder.RequireAdmin();

    protected override async Task<IResult> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var client = await request.Db.Clients.Find(c => c.ClientId == request.ClientId).FirstOrDefaultAsync(cancellationToken);
        if (client == null)
        {
            return Results.NotFound();
        }

        var secret = client.ClientSecrets.FirstOrDefault(s => SecretModel.Fingerprint(s) == request.Id);
        if (secret == null)
        {
            return Results.NotFound();
        }

        client.ClientSecrets.Remove(secret);
        await request.Db.UpsertAsync(client, cancellationToken);
        await Audit.RaiseAsync(request.Events, request.HttpContext, "Client secret removed", $"{client.ClientId}: {request.Id}", clientId: client.ClientId);
        return Results.NoContent();
    }
}

public static class ClientInput
{
    public static (Client? Client, string? Error) Parse(JsonElement body)
    {
        try
        {
            var client = AdminJson.Read<Client>(body);
            if (client == null)
            {
                return (null, "The body is not a client.");
            }

            if (string.IsNullOrWhiteSpace(client.ClientId))
            {
                return (null, "clientId is required.");
            }

            return (client, null);
        }
        catch (InvalidOperationException ex)
        {
            // Client.AllowedGrantTypes rejects invalid combinations.
            return (null, ex.Message);
        }
        catch (ArgumentException ex)
        {
            return (null, ex.Message);
        }
    }

    public static async Task<string?> ValidateAsync(IClientConfigurationValidator validator, Client client)
    {
        var context = new ClientConfigurationValidationContext(client);
        await validator.ValidateAsync(context);
        return context.IsValid ? null : context.ErrorMessage ?? "The client configuration is not valid.";
    }
}
