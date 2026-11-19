using System.Security.Cryptography;

namespace IdP.Users;

/// <summary>
/// Prepares the database at startup: indexes, the built-in roles, the first administrator, the standard identity
/// resources, the scope of the administration API and the configuration of <c>IdP:Seed</c>. Nothing that exists is changed.
/// </summary>
public sealed class Bootstrap(
    IdPMongoContext context,
    OidcServerMongoContext oidc,
    IServiceScopeFactory scopes,
    IOptions<IdPOptions> options,
    ILogger<Bootstrap> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var settings = options.Value;
        await context.CreateIndexesAsync(TimeSpan.FromDays(Math.Max(settings.Events.RetentionDays, 1)), cancellationToken);

        await EnsureRoleAsync(ApiAuth.AdminRole, "Administers the identity provider: users, clients, resources and settings.", cancellationToken);
        await EnsureResourcesAsync(settings.Seed, cancellationToken);
        await EnsureAdminAsync(settings.Admin, cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task EnsureRoleAsync(string name, string description, CancellationToken cancellationToken)
    {
        var normalized = name.ToUpperInvariant();
        if (await context.Roles.Find(r => r.NormalizedName == normalized).AnyAsync(cancellationToken))
        {
            return;
        }

        await context.Roles.InsertOneAsync(new IdPRole { Name = name, NormalizedName = normalized, Description = description, BuiltIn = true }, cancellationToken: cancellationToken);
    }

    private async Task EnsureResourcesAsync(IdPOptions.SeedOptions seed, CancellationToken cancellationToken)
    {
        IdentityResource[] standard =
        [
            new IdentityResources.OpenId(),
            new IdentityResources.Profile(),
            new IdentityResources.Email(),
            new IdentityResources.Phone(),
            new IdentityResource("roles", "Roles and groups", [JwtClaimTypes.Role, "groups"]) { Description = "The roles and groups of the user" }
        ];

        foreach (var resource in standard.Concat(seed.IdentityResources))
        {
            if (!await oidc.IdentityResources.Find(r => r.Name == resource.Name).AnyAsync(cancellationToken))
            {
                await oidc.UpsertAsync(resource, cancellationToken);
            }
        }

        ApiScope[] standardScopes =
        [
            new ApiScope(OidcServerConstants.LocalApi.ScopeName, "Administration API of the identity provider")
            {
                Description = "Calls the administration API with an access token (the admin role is still required).",
                UserClaims = { JwtClaimTypes.Role }
            }
        ];

        foreach (var scope in standardScopes.Concat(seed.ApiScopes))
        {
            if (!await oidc.ApiScopes.Find(s => s.Name == scope.Name).AnyAsync(cancellationToken))
            {
                await oidc.UpsertAsync(scope, cancellationToken);
            }
        }

        foreach (var api in seed.ApiResources)
        {
            if (!await oidc.ApiResources.Find(r => r.Name == api.Name).AnyAsync(cancellationToken))
            {
                await oidc.UpsertAsync(api, cancellationToken);
            }
        }

        foreach (var client in seed.Clients)
        {
            if (!await oidc.Clients.Find(c => c.ClientId == client.ClientId).AnyAsync(cancellationToken))
            {
                await oidc.UpsertAsync(client, cancellationToken);
                logger.LogInformation("Client {ClientId} created from IdP:Seed", client.ClientId);
            }
        }
    }

    private async Task EnsureAdminAsync(IdPOptions.AdminOptions admin, CancellationToken cancellationToken)
    {
        if (await context.Users.Find(u => u.Roles.Contains(ApiAuth.AdminRole)).AnyAsync(cancellationToken))
        {
            return;
        }

        using var scope = scopes.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<IdPUser>>();

        var password = admin.Password;
        var generated = false;
        if (string.IsNullOrEmpty(password))
        {
            password = Convert.ToBase64String(RandomNumberGenerator.GetBytes(18));
            generated = true;
        }

        var user = new IdPUser
        {
            UserName = admin.UserName,
            Email = admin.Email,
            EmailConfirmed = admin.Email != null,
            FirstName = "Administrator",
            Roles = [ApiAuth.AdminRole]
        };

        var result = await users.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            logger.LogError("The administrator {UserName} could not be created: {Errors}", admin.UserName, string.Join("; ", result.Errors.Select(e => e.Description)));
            return;
        }

        if (generated)
        {
            logger.LogWarning("Administrator {UserName} created with the generated password {Password}. Change it, or set IdP:Admin:Password.", admin.UserName, password);
        }
        else
        {
            logger.LogInformation("Administrator {UserName} created", admin.UserName);
        }
    }
}
