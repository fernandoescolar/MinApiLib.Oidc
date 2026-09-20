# Samples

Three applications that show a web application signing a user in with
[MinApiLib.OidcServer](../src/MinApiLib.OidcServer/README.md) and then calling an API with the user's access token.
The token is either a plain **bearer** token or a **DPoP**-bound token (proof of possession,
[RFC 9449](https://www.rfc-editor.org/rfc/rfc9449)).

| Application | URL | What it does |
|---|---|---|
| [Server](Server) | https://localhost:5001 | The OpenID Connect server, with login, logout and error pages. In memory, or on MongoDB |
| [WebClient](WebClient) | https://localhost:5002 | ASP.NET Core web application: OpenID Connect login (code flow with PKCE), calls the API |
| [Api](Api) | https://localhost:5003 | Orders API: validates the access token and, for DPoP, the proof |

The three are built with [MinApiLib](https://github.com/fernandoescolar/MinApiLib) endpoint records.

## Run

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

### With MongoDB

Start MongoDB with the compose file of this folder and run the server with its `https-mongo` profile:

```bash
docker compose up -d
```

```bash
dotnet run --project Server --launch-profile https-mongo
```

The server keeps its configuration, grants and signing keys in the `oidcserver-sample` database, using
[MinApiLib.OidcServer.MongoDB](../src/MinApiLib.OidcServer.MongoDB/README.md). It writes the clients and resources
of [Config.cs](Server/Config.cs) at startup. To use another MongoDB, set the `ConnectionStrings__Mongo`
environment variable instead of using the profile.

## What to try

The home page of the web client has two buttons. Both sign in the same user, through two clients of the server.

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

1. Right before redeeming the authorization code, the web client creates a P-256 key for the session
   ([Oidc.cs](WebClient/Oidc.cs)). It keeps the key, protected, in the session cookie.
2. It adds a `DPoP` proof of that key to the token request
   ([DPoPTokenRequestHandler.cs](WebClient/DPoP/DPoPTokenRequestHandler.cs)).
3. The server answers `"token_type": "DPoP"`, and the access token carries the key thumbprint in `cnf.jkt`. The home
   page shows the session key, its thumbprint and the token, so you can compare them.
4. Each API call sends the token with the `DPoP` scheme and a new proof, signed by the key, for that method and URL,
   with the token hash in `ath` ([DPoPProof.cs](WebClient/DPoP/DPoPProof.cs)):

   ```http
   GET /orders HTTP/1.1
   Host: localhost:5003
   Authorization: DPoP eyJhbGciOiJSUzI1NiIsImtpZCI6...
   DPoP: eyJhbGciOiJFUzI1NiIsImp3ayI6eyJrdHkiOiJFQyIs...
   ```

5. The API validates the token, then the proof: its signature with the `jwk` header, `htm`, `htu`, `iat`, `ath`, a
   `jti` not seen before, and that the thumbprint of its key equals `cnf.jkt`
   ([DPoPProofValidator.cs](Api/DPoP/DPoPProofValidator.cs), [DPoPJwtBearerEvents.cs](Api/DPoP/DPoPJwtBearerEvents.cs)).

The page has three more buttons, which play the attacker who stole the token:

| Button | What is sent | API answer |
|---|---|---|
| Send the token as a bearer token | `Authorization: Bearer <DPoP-bound token>` | `401`, `DPoP-bound access token sent without the DPoP scheme` |
| Sign the proof with another key | The token with a valid proof of a different key | `401`, `DPoP proof key does not match the key the access token is bound to` |
| Replay the proof | The same request and proof twice | `200`, then `401`, `DPoP proof has already been used` |

The [proof-of-possession documentation](../docs/topics/pop.md) walks through the same exchange message by message.

## What the samples leave out

They are kept short to show the protocol. A real deployment also needs:

- a real user store, such as ASP.NET Core Identity, and a consent page if clients require consent;
- a persistent store for production. The in-memory one loses grants on restart; use the MongoDB stores or your own;
- token refresh in the web client. The access token lasts an hour. With DPoP, the refresh request needs a proof
  of the same key;
- binding the authorization code to the key with `dpop_jkt`. The sample creates the key when the code arrives;
- a distributed replay cache in the API when it runs on several instances. The sample uses `IMemoryCache`.
