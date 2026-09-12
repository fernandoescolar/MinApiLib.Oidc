using MinApiLib.OidcServer.Core.Configuration;
using MinApiLib.OidcServer.Core.Extensions;
using MinApiLib.OidcServer.Core.Infrastructure;
using MinApiLib.OidcServer.Core.Stores;
using MinApiLib.OidcServer.Core.TokenServices;
using MinApiLib.OidcServer.Core.Users;
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for the OidcServer builder
/// </summary>
public static class OidcServerBuilderExtensions
{
    /// <summary>
    /// Adds test users.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="users">The users.</param>
    /// <returns></returns>
    public static IOidcServerBuilder AddTestUsers(this IOidcServerBuilder builder, List<TestUser> users)
    {
        builder.Services.AddSingleton(new TestUserStore(users));
        builder.AddProfileService<TestUserProfileService>();
        builder.AddResourceOwnerValidator<TestUserResourceOwnerPasswordValidator>();

        return builder;
    }
}
