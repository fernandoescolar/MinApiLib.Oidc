[[go back to index]](../index.md#documentation-structure)

# Packaging and Builds

IdentityServer consists of a number of nuget packages.

## IdentityServer4 main repo

[github](https://github.com/identityserver/IdentityServer4)

Contains the core IdentityServer object model, services and middleware as well as the EntityFramework and ASP.NET Identity integration.

nugets:

* [IdentityServer4](https://www.nuget.org/packages/IdentityServer4/)
* [IdentityServer4.EntityFramework](https://www.nuget.org/packages/IdentityServer4.EntityFramework)
* [IdentityServer4.AspNetIdentity](https://www.nuget.org/packages/IdentityServer4.AspNetIdentity)

## Quickstart UI

[github](https://github.com/IdentityServer/IdentityServer4.Quickstart.UI)

Contains a simple starter UI including login, logout and consent pages.

## Access token validation handler

[nuget](https://www.nuget.org/packages/IdentityServer4.AccessTokenValidation) | [github](https://github.com/IdentityServer/IdentityServer4.AccessTokenValidation)

ASP.NET Core authentication handler for validating tokens in APIs. The handler allows supporting both JWT and reference tokens in the same API.

## Templates

[nuget](https://www.nuget.org/packages/IdentityServer4.Templates) | [github](https://github.com/IdentityServer/IdentityServer4.Templates)

Contains templates for the dotnet CLI.

## Dev builds

In addition we publish CI builds to our package repository.
Add the following `nuget.config` to your project:

```csharp
<?xml version="1.0" encoding="utf-8"?>
    <configuration>
        <packageSources>
            <clear />
            <add key="IdentityServer CI" value="https://www.myget.org/F/identity/api/v3/index.json" />
        </packageSources>
    </configuration>
```

[[go back to index]](../index.md#documentation-structure)