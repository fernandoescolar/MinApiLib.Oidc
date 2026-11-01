[[go back to index]](../index.md#documentation-structure)

# Client Authentication

In certain situations, clients need to authenticate with the server, for example:

* confidential applications (aka clients) requesting tokens at the token endpoint;
* APIs validating reference tokens at the introspection endpoint.

For that purpose you can assign a list of secrets to a client or an API resource.

Secret parsing and validation is an extension point of the server. Out of the box it supports shared secrets, sent in
a basic authentication header (`client_secret_basic`) or in the POST body (`client_secret_post`). Private key JWTs
and mutual TLS are one builder call away. The types live in `MinApiLib.Oidc.Server.Core.ClientAuthentication`.

## Creating a shared secret

The following code sets up a hashed shared secret:

```csharp
var secret = new Secret("secret".Sha256());
```

This secret can now be assigned to either a `Client` or an `ApiResource`. Both accept several secrets, which is
useful for secret rollover and rotation:

```csharp
var client = new Client
{
    ClientId = "client",
    ClientSecrets = { secret },

    AllowedGrantTypes = GrantTypes.ClientCredentials,
    AllowedScopes = { "api1", "api2" }
};
```

You can also give a secret a description and an expiration date. The description is used for logging, and the
expiration date enforces a lifetime:

```csharp
var secret = new Secret(
    "secret".Sha256(),
    "2026 secret",
    new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc));
```

## Authentication using a shared secret

You can either send the client ID and secret in the POST body:

```http
POST /connect/token HTTP/1.1
Host: login.example.com
Content-Type: application/x-www-form-urlencoded

client_id=client&client_secret=secret&grant_type=client_credentials&scope=api1
```

...or in a basic authentication header, with the URL-encoded client ID and secret separated by a colon and encoded as
Base64:

```http
POST /connect/token HTTP/1.1
Host: login.example.com
Authorization: Basic Y2xpZW50OnNlY3JldA==
Content-Type: application/x-www-form-urlencoded

grant_type=client_credentials&scope=api1
```

In .NET, [MinApiLib.Oidc.Http](../../src/MinApiLib.Oidc.Http/README.md) sends the client credentials token request
for you, with the secret in the body, and the ASP.NET Core OpenID Connect handler authenticates the client when it
redeems the authorization code.

## Authentication using an asymmetric key

There are other techniques to authenticate clients, for example with public/private key cryptography. The server
supports private key JWT client assertions (`private_key_jwt`, see [RFC 7523](https://tools.ietf.org/html/rfc7523)
and the [OpenID Connect specification](https://openid.net/specs/openid-connect-core-1_0.html#ClientAuthentication)).

Secret extensibility typically consists of three things:

* a secret definition;
* a secret parser that extracts the secret from the incoming request (`ISecretParser`);
* a secret validator that validates the parsed secret against the definition (`ISecretValidator`).

Register your own with `AddSecretParser<T>()` and `AddSecretValidator<T>()`. For private key JWTs, one call adds
`JwtBearerClientAssertionSecretParser` and `PrivateKeyJwtSecretValidator`:

```csharp
builder.Services.AddOidcServer()
    .AddJwtBearerClientAuthentication();
```

The validator expects the client's public key on its secrets, either the full (leaf) certificate as Base64 or a JSON
web key (RSA or EC):

```csharp
var client = new Client
{
    ClientId = "client.jwt",
    ClientSecrets =
    {
        new Secret
        {
            Type = OidcServerConstants.SecretTypes.X509CertificateBase64,
            Value = "MIIDATCCAe2gAwIBAgIQoHUYAquk9rBJcq8W+F0FAzAJBgUrDgMCHQUAMBIxEDAOBgNVBAMTB0RldlJvb3Qw..."
        },
        new Secret
        {
            Type = OidcServerConstants.SecretTypes.JsonWebKey,
            Value = """{"kty":"EC","crv":"P-256","x":"p6DvwL4HSd_Ih3ij_YowBaI08yeAsI4dZ3eKK1d9oNI","y":"BGCg2XGMu0eTW6LDOFYehGQigAofRXfHbim4ogjOgUU"}"""
        }
    },

    AllowedGrantTypes = GrantTypes.ClientCredentials,
    AllowedScopes = { "api1", "api2" }
};
```

The client then sends a JWT signed with its private key instead of a secret:

```http
POST /connect/token HTTP/1.1
Host: login.example.com
Content-Type: application/x-www-form-urlencoded

grant_type=client_credentials
&scope=api1
&client_id=client.jwt
&client_assertion_type=urn%3Aietf%3Aparams%3Aoauth%3Aclient-assertion-type%3Ajwt-bearer
&client_assertion=eyJhbGciOiJFUzI1NiIsInR5cCI6IkpXVCJ9...
```

The assertion must have `iss` and `sub` equal to the client ID, the token endpoint URL or the issuer identifier as
`aud`, an `exp` and a `jti`. Each `jti` is accepted once. Discovery announces `private_key_jwt` in
`token_endpoint_auth_methods_supported`.

## Authentication using mutual TLS

With `AddMutualTlsSecretValidators()`, clients can authenticate with a TLS client certificate, identified by its
thumbprint (`X509CertificateThumbprint`) or its subject name (`X509CertificateName`). See [Mutual TLS](mtls.md).

[[go back to index]](../index.md#documentation-structure)
