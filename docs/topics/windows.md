[[go back to index]](../index.md#documentation-structure)

# Windows Authentication

ASP.NET Core has several ways to authenticate Windows users, and the login page of the server can use any of them:

* on Windows, hosted in IIS (in-process or out-of-process);
* on Windows, hosted in HTTP.sys;
* on any platform, with the Negotiate authentication handler (`Microsoft.AspNetCore.Authentication.Negotiate`).

This page shows IIS hosting. The pattern is the same with the others: authenticate the Windows user, then treat
the result as an [external sign-in](signin_external_providers.md).

## On Windows using IIS hosting

`WebApplication.CreateBuilder` enables IIS integration when the application is hosted in IIS. Turn on Windows
authentication in `launchSettings.json` or in the IIS configuration.

The IIS integration registers a Windows authentication handler. On a server it is better to turn off its automatic
behaviour, so only the login page triggers it:

```csharp
// in-process hosting
builder.Services.Configure<IISServerOptions>(iis =>
{
    iis.AuthenticationDisplayName = "Windows";
    iis.AutomaticAuthentication = false;
});

// ...or out-of-process hosting
builder.Services.Configure<IISOptions>(iis =>
{
    iis.AuthenticationDisplayName = "Windows";
    iis.AutomaticAuthentication = false;
});
```

A challenge to the `Windows` scheme (`Microsoft.AspNetCore.Server.IISIntegration.IISDefaults.AuthenticationScheme`)
starts Windows authentication: the response has a `WWW-Authenticate` header, and the browser loads the same URL again
with the Windows identity. Windows authentication succeeded when `AuthenticateAsync("Windows")` returns a
`WindowsPrincipal`.

The principal has the user and group SIDs and the Windows account name. This endpoint triggers Windows authentication
and, once it succeeded, writes a `ClaimsPrincipal` to the server's external cookie and continues with the external
sign-in callback:

```csharp
app.MapGet("/account/windows", async (HttpContext http, string? returnUrl) =>
{
    // has Windows authentication already been requested and succeeded?
    var result = await http.AuthenticateAsync("Windows");
    if (result.Principal is not WindowsPrincipal wp)
    {
        // trigger Windows authentication. It does not support a redirect URI, so the browser comes back to this URL
        return Results.Challenge(authenticationSchemes: ["Windows"]);
    }

    // treat Windows authentication like any other external sign-in
    var properties = new AuthenticationProperties
    {
        RedirectUri = "/account/external/callback",
        Items =
        {
            ["returnUrl"] = returnUrl ?? "/",
            ["scheme"] = "Windows"
        }
    };

    var id = new ClaimsIdentity("Windows");

    // the SID is a good sub value
    id.AddClaim(new Claim(JwtClaimTypes.Subject, wp.FindFirst(ClaimTypes.PrimarySid)!.Value));

    // the account name is the closest there is to a display name
    id.AddClaim(new Claim(JwtClaimTypes.Name, wp.Identity!.Name!));

    // the groups as role claims, with their display names. Beware of users in many groups
    var wi = (WindowsIdentity)wp.Identity;
    var groups = wi.Groups!.Translate(typeof(NTAccount));
    id.AddClaims(groups.Select(g => new Claim(JwtClaimTypes.Role, g.Value)));

    await http.SignInAsync(OidcServerConstants.ExternalCookieAuthenticationScheme, new ClaimsPrincipal(id), properties);
    return Results.Redirect(properties.RedirectUri);
});
```

The callback is the one of [external sign-in](signin_external_providers.md#handling-the-callback-and-signing-in-the-user):
it reads the external cookie, finds or creates the local user and signs the user in to the server.

[[go back to index]](../index.md#documentation-structure)
