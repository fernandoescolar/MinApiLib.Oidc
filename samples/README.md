# Samples

Applications that show a web application signing a user in with
[MinApiLib.Oidc.Server](../src/MinApiLib.Oidc.Server/README.md) and then calling an API with the user's access token,
and a background worker calling the same API with its own token. The token is either a plain **bearer** token or a
**DPoP**-bound token (proof of possession, [RFC 9449](https://www.rfc-editor.org/rfc/rfc9449)). The web client and the
worker get and send their tokens with [MinApiLib.Oidc.Authentication](../src/MinApiLib.Oidc.Authentication/README.md)
and [MinApiLib.Oidc.Http](../src/MinApiLib.Oidc.Http/README.md).

| Application | URL | What it does |
|---|---|---|
| [Server](Server) | https://localhost:5001 | The OpenID Connect server, with login (password or passkey), passkey, logout and error pages in React. In memory, or on MongoDB |
| [WebClient](WebClient) | https://localhost:5002 | ASP.NET Core web application with React pages: OpenID Connect login (code flow with PKCE), calls the API with the user's token |
| [Api](Api) | https://localhost:5003 | Orders API: validates the access token and, for DPoP, the proof, with `AddJwtBearer().AddDPoP()` |
| [Worker](Worker) | | Console application: calls the API with its own token (client credentials) and prints each request |

The web applications are built with [MinApiLib](https://github.com/fernandoescolar/MinApiLib) endpoint records, and
their pages with React (see [The frontend](#the-frontend)).

## Run

The Server and the WebClient need [Node.js](https://nodejs.org) 20.19 or later (or 22.12 or later) to build their
frontend: `dotnet build` and `dotnet run` install its packages and build it when it changes.

1. Trust the ASP.NET Core development certificate, once. The three applications talk to each other over HTTPS:

   ```bash
   dotnet dev-certs https --trust
   ```

   On Linux it ends with "There was an error trusting the HTTPS developer certificate", and the applications log that
   the certificate "is only trusted by some clients". The samples still work: the command trusts the certificate in
   the .NET user store, which the applications use to call each other, and in the browsers' certificate database.
   Only OpenSSL tools, such as `curl`, do not trust it. For those, set
   `SSL_CERT_DIR="$HOME/.aspnet/dev-certs/trust:/etc/ssl/certs"`.

2. Start each application in its own terminal, from this folder:

   ```bash
   dotnet run --project Server
   ```

   ```bash
   dotnet run --project Api
   ```

   ```bash
   dotnet run --project WebClient
   ```

3. Open https://localhost:5002 and sign in as `alice` / `alice` or `bob` / `bob`.

4. To run the worker, with the server and the API started:

   ```bash
   dotnet run --project Worker
   ```

### With MongoDB

Start MongoDB with the compose file of this folder and run the server with its `https-mongo` profile:

```bash
docker compose up -d
```

```bash
dotnet run --project Server --launch-profile https-mongo
```

The server keeps its configuration, grants and signing keys in the `oidcserver-sample` database, using
[MinApiLib.Oidc.Server.MongoDB](../src/MinApiLib.Oidc.Server.MongoDB/README.md). It writes the clients and resources
of [Config.cs](Server/Config.cs) at startup. To use another MongoDB, set the `ConnectionStrings__Mongo`
environment variable instead of using the profile.

## The frontend

The pages of the Server and the WebClient are a React + TypeScript + Tailwind application each, in `ClientApp/`,
built by [Vite](https://vite.dev) into `wwwroot/`. Every page is a single-page application that the backend renders
with the same index:

```html
<div id="app" app-id="login" app-model="eyJyZXR1cm5VcmwiOiIvY29ubmVjdC9hdXRob3JpemUv..."></div>
<script type="module" src="/assets/main-D8ahmgX5.js"></script>
```

`app-id` says which page to mount ([src/pages/index.ts](Server/ClientApp/src/pages/index.ts)), and `app-model` carries
its model as base64 UTF-8 JSON. An endpoint renders it with `spa.Render("login", "Sign in", model)`
([SpaPages.cs](Shared/SpaPages.cs), shared by both applications), which reads the hashed file names of the build from
Vite's manifest (`wwwroot/.vite/manifest.json`). The forms post to the backend as plain HTML forms, so the
antiforgery token, the redirects and the error pages work as in any server-rendered application.

The build runs Vite through [ClientApp.targets](ClientApp.targets): `npm ci` when `package-lock.json` changes, and
`npm run build` (type check and Vite) when the frontend's sources change. While working on a page, run
`npm run watch` in its `ClientApp/` folder and reload the browser: the backend picks up the new build without a
restart. To build without Node.js, pass `-p:SkipClientApp=true`; the pages then need an earlier build in `wwwroot/`.

To add a page, write its component in `src/pages/`, register it in `src/pages/index.ts` under an `app-id`, and render
that `app-id` from an endpoint with its model.

## What to try

The home page of the web client has two sign-in buttons. Both sign in the same user, through two clients of the server.
A third one signs in with a passkey (see [Passkeys](#passkeys)).

### Sign in (Bearer)

The client `web` gets a bearer access token. The home page shows the decoded token. **Call the API** sends:

```http
GET /orders HTTP/1.1
Host: localhost:5003
Authorization: Bearer eyJhbGciOiJSUzI1NiIsImtpZCI6...
```

The API validates the token (signature, issuer, `aud: orders`, lifetime, `scope: orders.read`) and answers with the
orders. Anyone who copies the token can do the same until it expires.

### Sign in (DPoP)

The client `web.dpop` has `RequireDPoP = true`: the server refuses token requests without a proof.

The web client registers the scheme with `AddOpenIdConnectTokens(Schemes.DPoP, options => options.UseDPoP = true)`
and the API client with `AddUserAccessTokenHandler()` ([Program.cs](WebClient/Program.cs)). The library does the rest:

1. When the sign-in starts, it creates a P-256 key for the session and sends its thumbprint in the authorization
   request (`dpop_jkt`). The authorization code is bound to the key. The key travels, protected, in the state, and
   then stays in the session cookie.
2. It redeems the code with a `DPoP` proof of that key.
3. The server answers `"token_type": "DPoP"`, and the access token carries the key thumbprint in `cnf.jkt`. The home
   page shows the session key, its thumbprint and the token, so you can compare them.
4. Each API call sends the token with the `DPoP` scheme and a new proof, signed by the key, for that method and URL,
   with the token hash in `ath`:

   ```http
   GET /orders HTTP/1.1
   Host: localhost:5003
   Authorization: DPoP eyJhbGciOiJSUzI1NiIsImtpZCI6...
   DPoP: eyJhbGciOiJFUzI1NiIsImp3ayI6eyJrdHkiOiJFQyIs...
   ```

5. The API validates the token, then the proof: its signature with the `jwk` header, `htm`, `htu`, `iat`, `ath`, a
   `jti` not seen before, and that the thumbprint of its key equals `cnf.jkt`. That is
   [MinApiLib.Oidc.JwtBearer](../src/MinApiLib.Oidc.JwtBearer/README.md), one line after `AddJwtBearer`
   ([Program.cs](Api/Program.cs)).

The access token lasts an hour. A minute before it expires, the library refreshes it with the refresh token and a
proof of the same key, and writes the new tokens to the session cookie.

The page has three more buttons, which play the attacker who stole the token. They build the requests by hand with
the library's `DPoPKey` and `DPoPProof` ([Api.cs](WebClient/Api.cs)):

| Button | What is sent | API answer |
|---|---|---|
| Send the token as a bearer token | `Authorization: Bearer <DPoP-bound token>` | `401`, `DPoP-bound access token sent without the DPoP scheme` |
| Sign the proof with another key | The token with a valid proof of a different key | `401`, `DPoP proof key does not match the key the access token is bound to` |
| Replay the proof | The same request and proof twice | `200`, then `401`, `DPoP proof has already been used` |

The [proof-of-possession documentation](../docs/topics/pop.md) walks through the same exchange message by message.

### Worker

The worker has two clients of the server, `worker` (bearer) and `worker.dpop` (`RequireDPoP`), and no user. It
registers each with `AddClientCredentialsTokens` and an HTTP client with `AddClientCredentialsTokenHandler`
([Program.cs](Worker/Program.cs)), and calls the API twice with each. The output shows the same token in both calls
and, for DPoP, a new proof each time:

```http
GET /orders HTTP/1.1
Host: localhost:5003
Authorization: DPoP eyJhbGciOiJSUzI1NiIsImtpZCI6...
DPoP: eyJhbGciOiJFUzI1NiIsImp3ayI6eyJrdHkiOiJF...
  proof payload: {"jti":"YX6oiCkLRVo2jBtFAHpmmw","htm":"GET","htu":"https://localhost:5003/orders","iat":1794854834,"ath":"IknNWzIa..."}

HTTP/1.1 200 OK
{ "user": null, "client": "worker.dpop", "tokenScheme": "DPoP", "boundToKey": "T-98tzp8BzD9FCouaCQ28btQdYTmw_igGv9TXJmQdVo", ... }
```

### Passkeys

The server's users live in ASP.NET Core Identity ([Users/](Server/Users)), which since .NET 10 handles passkeys
(WebAuthn): `IPasskeyHandler` makes the options for the browser and checks its answers, signature, challenge, origin
and counter included. The server keeps the state of each ceremony in an encrypted cookie for five minutes
([PasskeyState.cs](Server/Passkeys/PasskeyState.cs)).

1. Open https://localhost:5001/account/passkeys, sign in as `bob` / `bob` and click **Add a passkey**. The browser
   creates it with the device's authenticator or your password manager.
2. On the web client, click **Sign in with a passkey (DPoP, `acr_values=phr`)**. The client asks for a
   phishing-resistant sign-in, so the server's login page only offers the passkey and refuses a password. The
   server has `EnforceAcrValues` on, so the session you opened with a password in step 1 is not enough: the server
   sends you to the login page again, and would answer `unmet_authentication_requirements` to the client if the
   sign-in were still not a passkey.
3. Back on the web client, the home page shows how the user signed in, from the identity token:
   `amr` = `hwk, mfa` (a key on this device, and the authenticator verified the user) and `acr` = `phr`. A synced
   passkey says `swk` instead of `hwk`. The access token carries the same claims, for the API.

The normal login page offers the passkey too, as a button and in the user name field's autofill. The sign-in with
the key happens in [PasskeyEndpoints.cs](Server/Pages/PasskeyEndpoints.cs), and [Login.cs](Server/Login.cs) decides
`amr` and `acr`. The web client keeps `acr` with `options.ClaimActions.Remove("acr")`, because the OpenID Connect
handler drops it by default.

The sample keeps users and passkeys in memory: a restart forgets the passkeys, so also remove them from the browser
or password manager.

## What the samples leave out

They are kept short to show the protocol. A real deployment also needs:

- a persistent user store, such as the Entity Framework Core stores of ASP.NET Core Identity, and a consent page if
  clients require consent;
- a persistent store for production. The in-memory one loses grants on restart; use the MongoDB stores or your own;
- a distributed replay cache in the API when it runs on several instances (`DistributedDPoPReplayCache`). The sample
  keeps the proofs it has seen in memory.
