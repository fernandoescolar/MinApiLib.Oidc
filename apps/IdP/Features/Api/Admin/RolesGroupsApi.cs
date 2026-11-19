namespace IdP.Features.Api.Admin;

// Roles (/api/admin/roles) and groups (/api/admin/groups).

public sealed record RoleModel(string Id, string Name, string? Description, bool BuiltIn, long Users);

public sealed record RoleInput(string Name, string? Description);

public sealed record ListRoles() : GetHandlerAsync<ListRoles.Request>("/api/admin/roles")
{
    public readonly record struct Request([FromServices] IdPMongoContext Db);

    protected override RouteHandlerBuilder Configure(RouteHandlerBuilder builder) => builder.RequireAdmin();

    protected override async Task<IResult> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var roles = await request.Db.Roles.Find(FilterDefinition<IdPRole>.Empty).SortBy(r => r.NormalizedName).ToListAsync(cancellationToken);
        var models = new List<RoleModel>();
        foreach (var role in roles)
        {
            var users = await request.Db.Users.CountDocumentsAsync(Builders<IdPUser>.Filter.AnyEq(u => u.Roles, role.Name), cancellationToken: cancellationToken);
            models.Add(new RoleModel(role.Id, role.Name, role.Description, role.BuiltIn, users));
        }

        return Results.Ok(models);
    }
}

public sealed record CreateRole() : PostHandlerAsync<CreateRole.Request>("/api/admin/roles")
{
    public readonly record struct Request(HttpContext HttpContext, [FromBody] RoleInput Body, [FromServices] IdPMongoContext Db, [FromServices] IEventService Events);

    protected override RouteHandlerBuilder Configure(RouteHandlerBuilder builder) => builder.RequireAdmin();

    protected override async Task<IResult> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var name = request.Body.Name?.Trim();
        if (string.IsNullOrEmpty(name) || name.Length > 100)
        {
            return ApiErrors.Validation("The role needs a name.", "name");
        }

        var role = new IdPRole { Name = name, NormalizedName = name.ToUpperInvariant(), Description = request.Body.Description?.Trim() };
        try
        {
            await request.Db.Roles.InsertOneAsync(role, cancellationToken: cancellationToken);
        }
        catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            return ApiErrors.Validation("A role with that name already exists.", "name");
        }

        await Audit.RaiseAsync(request.Events, request.HttpContext, "Role created", name);
        return Results.Json(new RoleModel(role.Id, role.Name, role.Description, role.BuiltIn, 0), Json.Options, statusCode: StatusCodes.Status201Created);
    }
}

public sealed record UpdateRole() : PutHandlerAsync<UpdateRole.Request>("/api/admin/roles/{id}")
{
    public readonly record struct Request(HttpContext HttpContext, [FromRoute] string Id, [FromBody] RoleInput Body, [FromServices] IdPMongoContext Db, [FromServices] IEventService Events);

    protected override RouteHandlerBuilder Configure(RouteHandlerBuilder builder) => builder.RequireAdmin();

    protected override async Task<IResult> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var role = await request.Db.Roles.Find(r => r.Id == request.Id).FirstOrDefaultAsync(cancellationToken);
        if (role == null)
        {
            return Results.NotFound();
        }

        var name = request.Body.Name?.Trim();
        if (string.IsNullOrEmpty(name))
        {
            return ApiErrors.Validation("The role needs a name.", "name");
        }

        if (role.BuiltIn && name != role.Name)
        {
            return ApiErrors.Validation("A built-in role cannot be renamed.", "name");
        }

        var oldName = role.Name;
        role.Name = name;
        role.NormalizedName = name.ToUpperInvariant();
        role.Description = request.Body.Description?.Trim();
        try
        {
            await request.Db.Roles.ReplaceOneAsync(r => r.Id == role.Id, role, cancellationToken: cancellationToken);
        }
        catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            return ApiErrors.Validation("A role with that name already exists.", "name");
        }

        if (oldName != name)
        {
            // The users and groups keep the name: rename it there too.
            await request.Db.Users.UpdateManyAsync(Builders<IdPUser>.Filter.AnyEq(u => u.Roles, oldName), Builders<IdPUser>.Update.Set("Roles.$", name), cancellationToken: cancellationToken);
            await request.Db.Groups.UpdateManyAsync(Builders<IdPGroup>.Filter.AnyEq(g => g.Roles, oldName), Builders<IdPGroup>.Update.Set("Roles.$", name), cancellationToken: cancellationToken);
        }

        await Audit.RaiseAsync(request.Events, request.HttpContext, "Role updated", name);
        var users = await request.Db.Users.CountDocumentsAsync(Builders<IdPUser>.Filter.AnyEq(u => u.Roles, name), cancellationToken: cancellationToken);
        return Results.Ok(new RoleModel(role.Id, role.Name, role.Description, role.BuiltIn, users));
    }
}

public sealed record DeleteRole() : DeleteHandlerAsync<DeleteRole.Request>("/api/admin/roles/{id}")
{
    public readonly record struct Request(HttpContext HttpContext, [FromRoute] string Id, [FromServices] IdPMongoContext Db, [FromServices] IEventService Events);

    protected override RouteHandlerBuilder Configure(RouteHandlerBuilder builder) => builder.RequireAdmin();

    protected override async Task<IResult> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var role = await request.Db.Roles.Find(r => r.Id == request.Id).FirstOrDefaultAsync(cancellationToken);
        if (role == null)
        {
            return Results.NotFound();
        }

        if (role.BuiltIn)
        {
            return ApiErrors.Validation("A built-in role cannot be deleted.");
        }

        await request.Db.Roles.DeleteOneAsync(r => r.Id == role.Id, cancellationToken);
        await request.Db.Users.UpdateManyAsync(Builders<IdPUser>.Filter.AnyEq(u => u.Roles, role.Name), Builders<IdPUser>.Update.Pull(u => u.Roles, role.Name), cancellationToken: cancellationToken);
        await request.Db.Groups.UpdateManyAsync(Builders<IdPGroup>.Filter.AnyEq(g => g.Roles, role.Name), Builders<IdPGroup>.Update.Pull(g => g.Roles, role.Name), cancellationToken: cancellationToken);
        await Audit.RaiseAsync(request.Events, request.HttpContext, "Role deleted", role.Name);
        return Results.NoContent();
    }
}

public sealed record GroupModel(string Id, string Name, string? Description, IReadOnlyList<string> Roles, IReadOnlyDictionary<string, string> Attributes, long Members, DateTime CreatedAt);

public sealed record GroupInput(string Name, string? Description, List<string>? Roles, Dictionary<string, string>? Attributes);

public static class GroupModels
{
    public static async Task<GroupModel> ModelAsync(IdPGroup group, IdPMongoContext db, CancellationToken cancellationToken)
        => new(group.Id, group.Name, group.Description, group.Roles, group.Attributes,
            await db.Users.CountDocumentsAsync(Builders<IdPUser>.Filter.AnyEq(u => u.Groups, group.Id), cancellationToken: cancellationToken), group.CreatedAt);
}

public sealed record ListGroups() : GetHandlerAsync<ListGroups.Request>("/api/admin/groups")
{
    public readonly record struct Request([FromServices] IdPMongoContext Db);

    protected override RouteHandlerBuilder Configure(RouteHandlerBuilder builder) => builder.RequireAdmin();

    protected override async Task<IResult> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var groups = await request.Db.Groups.Find(FilterDefinition<IdPGroup>.Empty).SortBy(g => g.Name).ToListAsync(cancellationToken);
        var models = new List<GroupModel>();
        foreach (var group in groups)
        {
            models.Add(await GroupModels.ModelAsync(group, request.Db, cancellationToken));
        }

        return Results.Ok(models);
    }
}

public sealed record GetGroup() : GetHandlerAsync<GetGroup.Request>("/api/admin/groups/{id}")
{
    public readonly record struct Request([FromRoute] string Id, [FromServices] IdPMongoContext Db);

    protected override RouteHandlerBuilder Configure(RouteHandlerBuilder builder) => builder.RequireAdmin();

    protected override async Task<IResult> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var group = await request.Db.Groups.Find(g => g.Id == request.Id).FirstOrDefaultAsync(cancellationToken);
        if (group == null)
        {
            return Results.NotFound();
        }

        var members = await request.Db.Users.Find(Builders<IdPUser>.Filter.AnyEq(u => u.Groups, group.Id)).SortBy(u => u.NormalizedUserName).Limit(500).ToListAsync(cancellationToken);
        return Results.Ok(new { group = await GroupModels.ModelAsync(group, request.Db, cancellationToken), members = members.Select(UserModels.Summary) });
    }
}

public sealed record CreateGroup() : PostHandlerAsync<CreateGroup.Request>("/api/admin/groups")
{
    public readonly record struct Request(HttpContext HttpContext, [FromBody] GroupInput Body, [FromServices] IdPMongoContext Db, [FromServices] IEventService Events);

    protected override RouteHandlerBuilder Configure(RouteHandlerBuilder builder) => builder.RequireAdmin();

    protected override async Task<IResult> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var name = request.Body.Name?.Trim();
        if (string.IsNullOrEmpty(name))
        {
            return ApiErrors.Validation("The group needs a name.", "name");
        }

        var error = await UserModels.CheckRolesAndGroupsAsync(request.Db, request.Body.Roles, null, cancellationToken);
        if (error != null)
        {
            return ApiErrors.Validation(error, "roles");
        }

        var group = new IdPGroup { Name = name, Description = request.Body.Description?.Trim(), Roles = request.Body.Roles ?? [], Attributes = request.Body.Attributes ?? new Dictionary<string, string>(StringComparer.Ordinal) };
        try
        {
            await request.Db.Groups.InsertOneAsync(group, cancellationToken: cancellationToken);
        }
        catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            return ApiErrors.Validation("A group with that name already exists.", "name");
        }

        await Audit.RaiseAsync(request.Events, request.HttpContext, "Group created", name);
        return Results.Json(await GroupModels.ModelAsync(group, request.Db, cancellationToken), Json.Options, statusCode: StatusCodes.Status201Created);
    }
}

public sealed record UpdateGroup() : PutHandlerAsync<UpdateGroup.Request>("/api/admin/groups/{id}")
{
    public readonly record struct Request(HttpContext HttpContext, [FromRoute] string Id, [FromBody] GroupInput Body, [FromServices] IdPMongoContext Db, [FromServices] IEventService Events);

    protected override RouteHandlerBuilder Configure(RouteHandlerBuilder builder) => builder.RequireAdmin();

    protected override async Task<IResult> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var group = await request.Db.Groups.Find(g => g.Id == request.Id).FirstOrDefaultAsync(cancellationToken);
        if (group == null)
        {
            return Results.NotFound();
        }

        var name = request.Body.Name?.Trim();
        if (string.IsNullOrEmpty(name))
        {
            return ApiErrors.Validation("The group needs a name.", "name");
        }

        var error = await UserModels.CheckRolesAndGroupsAsync(request.Db, request.Body.Roles, null, cancellationToken);
        if (error != null)
        {
            return ApiErrors.Validation(error, "roles");
        }

        group.Name = name;
        group.Description = request.Body.Description?.Trim();
        group.Roles = request.Body.Roles ?? group.Roles;
        group.Attributes = request.Body.Attributes ?? group.Attributes;
        try
        {
            await request.Db.Groups.ReplaceOneAsync(g => g.Id == group.Id, group, cancellationToken: cancellationToken);
        }
        catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            return ApiErrors.Validation("A group with that name already exists.", "name");
        }

        await Audit.RaiseAsync(request.Events, request.HttpContext, "Group updated", name);
        return Results.Ok(await GroupModels.ModelAsync(group, request.Db, cancellationToken));
    }
}

public sealed record DeleteGroup() : DeleteHandlerAsync<DeleteGroup.Request>("/api/admin/groups/{id}")
{
    public readonly record struct Request(HttpContext HttpContext, [FromRoute] string Id, [FromServices] IdPMongoContext Db, [FromServices] IEventService Events);

    protected override RouteHandlerBuilder Configure(RouteHandlerBuilder builder) => builder.RequireAdmin();

    protected override async Task<IResult> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var result = await request.Db.Groups.DeleteOneAsync(g => g.Id == request.Id, cancellationToken);
        if (result.DeletedCount == 0)
        {
            return Results.NotFound();
        }

        await request.Db.Users.UpdateManyAsync(Builders<IdPUser>.Filter.AnyEq(u => u.Groups, request.Id), Builders<IdPUser>.Update.Pull(u => u.Groups, request.Id), cancellationToken: cancellationToken);
        await Audit.RaiseAsync(request.Events, request.HttpContext, "Group deleted", request.Id);
        return Results.NoContent();
    }
}

public sealed record AddGroupMember() : PostHandlerAsync<AddGroupMember.Request>("/api/admin/groups/{id}/members")
{
    public sealed record Body(string UserId);

    public readonly record struct Request(HttpContext HttpContext, [FromRoute] string Id, [FromBody] Body Body, [FromServices] IdPMongoContext Db, [FromServices] IEventService Events);

    protected override RouteHandlerBuilder Configure(RouteHandlerBuilder builder) => builder.RequireAdmin();

    protected override async Task<IResult> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        if (!await request.Db.Groups.Find(g => g.Id == request.Id).AnyAsync(cancellationToken))
        {
            return Results.NotFound();
        }

        var result = await request.Db.Users.UpdateOneAsync(u => u.Id == request.Body.UserId, Builders<IdPUser>.Update.AddToSet(u => u.Groups, request.Id), cancellationToken: cancellationToken);
        if (result.MatchedCount == 0)
        {
            return ApiErrors.Validation("Unknown user.", "userId");
        }

        await Audit.RaiseAsync(request.Events, request.HttpContext, "Group member added", request.Id, request.Body.UserId);
        return Results.NoContent();
    }
}

public sealed record RemoveGroupMember() : DeleteHandlerAsync<RemoveGroupMember.Request>("/api/admin/groups/{id}/members/{userId}")
{
    public readonly record struct Request(HttpContext HttpContext, [FromRoute] string Id, [FromRoute] string UserId, [FromServices] IdPMongoContext Db, [FromServices] IEventService Events);

    protected override RouteHandlerBuilder Configure(RouteHandlerBuilder builder) => builder.RequireAdmin();

    protected override async Task<IResult> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        await request.Db.Users.UpdateOneAsync(u => u.Id == request.UserId, Builders<IdPUser>.Update.Pull(u => u.Groups, request.Id), cancellationToken: cancellationToken);
        await Audit.RaiseAsync(request.Events, request.HttpContext, "Group member removed", request.Id, request.UserId);
        return Results.NoContent();
    }
}
