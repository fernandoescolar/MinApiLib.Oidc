# MinApiLib IdP

An OpenID Connect identity provider in one application: the server of
[MinApiLib.Oidc.Server](../../src/MinApiLib.Oidc.Server/README.md) with its MongoDB stores, users with passwords,
authenticator apps (TOTP) and passkeys, sign-in through external OpenID Connect providers, an account console for the
users, an administration console and a JSON administration API. The pages are React + TypeScript + Tailwind, built by
Vite. It ships as a Docker image, so it can be run as it is, like Keycloak, and configured through environment
variables.

```bash
docker run -p 8080:8080 --add-host=host.docker.internal:host-gateway \
  -e ConnectionStrings__Mongo=mongodb://host.docker.internal:27017 \
  -e IdP__Admin__Password=ChangeMe-123 \
  ghcr.io/fernandoescolar/minapilib-oidc-idp
```

Open http://localhost:8080, sign in as `admin` with that password, and the administration console is at `/admin`.
[docker-compose.yml](docker-compose.yml) starts it together with MongoDB:

```bash
docker compose -f apps/IdP/docker-compose.yml up
```

## What it does

| Area | Features |
|---|---|
| Protocol | Everything in `MinApiLib.Oidc.Server`: authorization code with PKCE, client credentials, refresh tokens, device flow, hybrid, PAR, DPoP, resource indicators, introspection, revocation, userinfo, front- and back-channel logout, automatic signing key rotation. Discovery at `/.well-known/openid-configuration` |
| Sign-in | User name or e-mail and password, with lockout; a second factor with an authenticator app (TOTP) and recovery codes; passkeys (WebAuthn), as the only factor and for `acr_values=phr`; external OpenID Connect providers (Google, Microsoft Entra ID, another instance of this server, ...), configured at run time; `acr_values=mfa` to require a second factor; `acr_values=idp:<alias>` to skip the login page |
| Accounts | Self-registration (optional, with or without e-mail verification), e-mail verification, forgotten password, consent page with remembered consents, device verification page |
| Account console (`/account`) | Profile and free attributes, e-mail change with verification, password, authenticator app, passkeys, applications with access (revoke), linked external accounts, sign out everywhere, own activity |
| Administration console (`/admin`) | Dashboard; clients (every setting of the server's client model, secrets shown once); identity resources, API scopes and API resources; users (credentials, roles, groups, claims, attributes, passkeys, linked accounts, sessions, activity, disable, delete); roles; groups (roles and members); external identity providers; sessions and tokens (revoke); signing keys; audit events with filters |
| Administration API (`/api/admin`) | The same operations as JSON. From the console, with the session cookie and an antiforgery header; from scripts, with an access token of this server for the `OidcServerApi` scope issued to a user with the `admin` role |
| Operations | MongoDB for everything, data protection keys included, so several instances can run together; `/health`; events kept for a configurable number of days; forwarded headers for reverse proxies; e-mails through SMTP, or to the log when no server is configured |

The users' claims come from the user document: `sub`, `name`, `given_name`, `family_name`, `preferred_username`,
`email`, `email_verified`, `phone_number`, `phone_number_verified`, `role` (the user's roles and those of its groups),
`groups`, every free attribute (`picture`, `locale`, `department`, ...) under its own name, and the extra claims an
administrator adds. The standard identity resources (`openid`, `profile`, `email`, `phone`) and a `roles` resource
(`role`, `groups`) are created at startup; a client gets a claim when one of its allowed scopes includes it.

## Configuration

Everything is in the `IdP` section of `appsettings.json`, so as environment variables the keys are `IdP__...`.

| Setting | Default | Meaning |
|---|---|---|
| `ConnectionStrings__Mongo` | `mongodb://localhost:27017` | The MongoDB connection string. Required |
| `IdP__Database` | `idp` | The database. The server's stores and the identity provider's collections share it |
| `IdP__Name` | `MinApiLib IdP` | Shown on the pages, in the e-mails and as the TOTP issuer |
| `IdP__Issuer` | request URL | The issuer (`iss`) and base of the endpoints. Set it to the public URL behind a reverse proxy |
| `IdP__ForwardedHeaders` | `true` | Honour `X-Forwarded-For/Proto/Host` |
| `IdP__CookieSameSite` | `Lax` | `SameSite` of the session cookies. `None` lets the check-session iframe work from other sites, and needs HTTPS |
| `IdP__Admin__UserName`, `IdP__Admin__Password`, `IdP__Admin__Email` | `admin`, generated, none | The first administrator, created when nobody has the `admin` role. A generated password is written to the log once |
| `IdP__Registration__Enabled` | `true` | Users can create their own account |
| `IdP__Registration__RequireEmailVerification` | `false` | A new account must verify its e-mail before signing in |
| `IdP__Password__RequiredLength`, `RequireDigit`, `RequireUppercase`, `RequireLowercase`, `RequireNonAlphanumeric` | `8`, `false`... | The password rules |
| `IdP__Password__MaxFailedAttempts`, `IdP__Password__LockoutMinutes` | `5`, `5` | Lockout |
| `IdP__Email__From`, `IdP__Email__FromName` | `no-reply@localhost` | The sender |
| `IdP__Email__Smtp__Host`, `Port`, `UserName`, `Password`, `UseSsl` | none, `587` | The SMTP server. Without a host, e-mails are written to the log |
| `IdP__Events__RetentionDays` | `30` | How long the audit events are kept |
| `IdP__Seed__Clients`, `IdentityResources`, `ApiScopes`, `ApiResources` | empty | Configuration created at startup when it does not exist (see below) |

The protocol options of the server (`OidcServerOptions`) are set in [Program.cs](Program.cs): automatic key
management on, `EnforceAcrValues` on, every event raised, and the pages at `/login`, `/logout`, `/consent`, `/error`
and `/device`.

### Seeding clients

`IdP:Seed` takes the server's models, so a client can come with the deployment. Documents that already exist are not
touched: once the server runs, the administration console is the source of truth.

```json
{
  "IdP": {
    "Seed": {
      "Clients": [
        {
          "ClientId": "web",
          "ClientName": "My web application",
          "ClientSecrets": [{ "Value": "<base64 of the SHA-256 of the secret>" }],
          "AllowedGrantTypes": ["authorization_code"],
          "RedirectUris": ["https://app.example.com/signin-oidc"],
          "PostLogoutRedirectUris": ["https://app.example.com/signout-callback-oidc"],
          "AllowedScopes": ["openid", "profile", "email", "roles"],
          "AllowOfflineAccess": true
        }
      ],
      "ApiScopes": [{ "Name": "orders.read", "DisplayName": "Read your orders" }]
    }
  }
}
```

Secret values are stored hashed, as the server expects (`echo -n secret | sha256sum | xxd -r -p | base64`). The
administration console generates secrets and shows them once.

### The administration API from a script

Create a client with the `client_credentials` grant? No: the API needs a user with the `admin` role. Give a client the
`authorization_code` grant and the `OidcServerApi` scope, sign in as an administrator, and call the API with the
access token:

```http
GET /api/admin/users?q=alice HTTP/1.1
Authorization: Bearer eyJ...
```

The token carries `role: admin` because the `OidcServerApi` scope asks for the `role` claim.

## Running from source

Requirements: .NET SDK 11 and Node.js 20.19 or later. With Docker, [Aspire](https://aspire.dev) starts MongoDB and
the IdP together, from the `apps/` folder:

```bash
dotnet run apphost.cs
```

[apphost.cs](../apphost.cs) is a single-file program: it starts MongoDB in a container, the IdP on it once the
database is up, and prints the URL of the Aspire dashboard, with the IdP's logs and traces. Ctrl+C stops both; the
database stays in the `minapilib-oidc-idp-mongodb` Docker volume (`docker volume rm minapilib-oidc-idp-mongodb`
starts it again from scratch).

Or, with a MongoDB of your own on `mongodb://localhost:27017` (`docker run -p 27017:27017 mongo:8`), from the
repository root:

```bash
dotnet run --project apps/IdP
```

Either way the `Development` environment sets the administrator password to `admin123`
([appsettings.Development.json](appsettings.Development.json)) and the server listens on https://localhost:5100. The
build installs the frontend's packages and builds it when its sources change; while working on the pages, run
`npm run watch` in [ClientApp/](ClientApp) and reload.

### Layout

| Folder | Contents |
|---|---|
| [Program.cs](Program.cs), [IdPOptions.cs](IdPOptions.cs) | The host and its configuration |
| [Users/](Users) | The user, role and group documents, the ASP.NET Core Identity store over MongoDB, the profile service (claims), sign-in (`amr`, `acr`, `idp`), the security stamp check of the session cookie, and the startup bootstrap |
| [Features/Flow/](Features/Flow) | The pages of the sign-in flow: login, second factor, passkeys, external providers, consent, logout, error, registration, e-mail verification, forgotten password, device verification, and the shells of the two consoles |
| [Features/Api/Account/](Features/Api/Account) | The JSON API of the account console |
| [Features/Api/Admin/](Features/Api/Admin) | The administration API: clients, resources, users, roles, groups, grants, keys, events, identity providers, dashboard |
| [Features/Events/](Features/Events) | The event sink that writes the server's events and the identity provider's audit events to MongoDB |
| [Features/IdentityProviders/](Features/IdentityProviders) | External providers: the document and the manager that registers an OpenID Connect handler for each of them at run time |
| [Infrastructure/](Infrastructure) | The page renderer, the API authentication filter (cookie + antiforgery header, or local API bearer token), data protection keys in MongoDB, e-mail |
| [ClientApp/](ClientApp) | The React frontend: `src/flow` (the sign-in pages), `src/account` (the account console), `src/admin` (the administration console), `src/components/ui.tsx` (the UI kit), `src/api.ts` (the JSON client) |

Every page is the same HTML: `<div id="app" app-id="login" app-model="...">` plus the script of the build. The
sign-in pages are plain forms that post to the backend, so redirects, antiforgery and the authorize request's
`returnUrl` work as in any server-rendered application; the consoles are client-side applications over the JSON APIs.

### Docker image

The image is built from the repository root, because the project references the libraries in `src/`:

```bash
docker build -f apps/IdP/Dockerfile -t minapilib-oidc-idp .
```

[docker.yml](../../.github/workflows/docker.yml) publishes it to `ghcr.io/fernandoescolar/minapilib-oidc-idp` on
every push to `main` (`latest`, `sha-<commit>`) and on every `X.Y.Z` tag (`X.Y.Z`, `X.Y`), for `linux/amd64` and
`linux/arm64`. The container listens on port 8080, runs as a non-root user and answers `GET /health`.

## Limitations

- One realm per deployment: one issuer, one set of users and clients. Run one container per tenant.
- External providers speak OpenID Connect. SAML and plain OAuth 2.0 providers are not supported.
- The external providers of another instance are picked up within a minute, when the manager re-reads the database.
