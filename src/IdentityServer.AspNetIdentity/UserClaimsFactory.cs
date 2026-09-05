using IdentityServer.Configuration.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace IdentityServer.AspNetIdentity;

internal sealed class UserClaimsFactory<TUser> : IUserClaimsPrincipalFactory<TUser>
    where TUser : class
{
    private readonly IUserClaimsPrincipalFactory<TUser> _inner;
    private readonly UserManager<TUser> _userManager;

    public UserClaimsFactory([FromKeyedServices(DecoratorServiceCollectionExtensions.InnerServiceKey)] IUserClaimsPrincipalFactory<TUser> inner, UserManager<TUser> userManager)
    {
        _inner = inner;
        _userManager = userManager;
    }

    public async Task<ClaimsPrincipal> CreateAsync(TUser user)
    {
        var principal = await _inner.CreateAsync(user);
        var identity = principal.Identities.First();

        if (!identity.HasClaim(x => x.Type == JwtClaimTypes.Subject))
        {
            var sub = await _userManager.GetUserIdAsync(user);
            identity.AddClaim(new Claim(JwtClaimTypes.Subject, sub));
        }

        var username = await _userManager.GetUserNameAsync(user);
        var usernameClaim = identity.FindFirst(claim => claim.Type == _userManager.Options.ClaimsIdentity.UserNameClaimType && claim.Value == username);
        if (usernameClaim != null && username != null)
        {
            identity.RemoveClaim(usernameClaim);
            identity.AddClaim(new Claim(JwtClaimTypes.PreferredUserName, username));
        }

        if (username != null && !identity.HasClaim(x => x.Type == JwtClaimTypes.Name))
        {
            identity.AddClaim(new Claim(JwtClaimTypes.Name, username));
        }

        if (_userManager.SupportsUserEmail)
        {
            var email = identity.FindFirst(x => x.Type == JwtClaimTypes.Email)?.Value
                        ?? await _userManager.GetEmailAsync(user);
            if (!String.IsNullOrWhiteSpace(email))
            {
                var emailConfimed = await _userManager.IsEmailConfirmedAsync(user) ? "true" : "false";
                identity.AddClaims(new[] {
                        new Claim(JwtClaimTypes.Email, email),
                        new Claim(JwtClaimTypes.EmailVerified,emailConfimed, ClaimValueTypes.Boolean)
                    });
            }
        }

        if (_userManager.SupportsUserPhoneNumber)
        {
            var phoneNumber = await _userManager.GetPhoneNumberAsync(user);
            if (!String.IsNullOrWhiteSpace(phoneNumber))
            {
                identity.AddClaims(new[]
                {
                        new Claim(JwtClaimTypes.PhoneNumber, phoneNumber),
                        new Claim(JwtClaimTypes.PhoneNumberVerified,
                            await _userManager.IsPhoneNumberConfirmedAsync(user) ? "true" : "false", ClaimValueTypes.Boolean)
                    });
            }
        }

        return principal;
    }
}
