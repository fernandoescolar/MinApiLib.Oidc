[[go back to index]](../index.md#documentation-structure)

# Authorize Request Objects

Instead of sending the parameters of an authorize request as individual query string values, a client can package
them in a signed JWT, the request object ([RFC 9101](https://www.rfc-editor.org/rfc/rfc9101), JAR). This makes the
parameters tamper-proof and authenticates the client already on the front channel.

The client sends it by value, in the `request` parameter, or by reference, as a URL in `request_uri` (see the
[specification](https://openid.net/specs/openid-connect-core-1_0.html#JWTRequests) for details).
[Pushed authorization requests](par.md) are another way to keep the parameters out of the browser, without signing.

The server requires request objects to be signed, with the client as issuer (`iss` = `client_id`), the server's
issuer name as audience (`aud`) and an expiration time (`exp`). The request object must contain `client_id`, which
must match the request's, and a parameter sent both in the request object and in the query must have the same value.
With `OidcServerOptions.StrictJarValidation` the JWT `typ` header must be `oauth-authz-req+jwt`.

The keys that verify the signature are the client's secrets of type X.509 certificate or JSON web key:

```csharp
var client = new Client
{
    ClientId = "foo",

    // set this to true to accept signed requests only
    RequireRequestObject = true,

    ClientSecrets =
    {
        new Secret
        {
            // X.509 certificate, base64-encoded
            Type = OidcServerConstants.SecretTypes.X509CertificateBase64,
            Value = Convert.ToBase64String(cert.Export(X509ContentType.Cert))
        },
        new Secret
        {
            // RSA key as JWK
            Type = OidcServerConstants.SecretTypes.JsonWebKey,
            Value =
                "{'e':'AQAB','kid':'ZzAjSnraU3bkWGnnAqLapYGpTyNfLbjbzgAPbbW2GEA','kty':'RSA','n':'wWwQFtSzeRjjerpEM5Rmqz_DsNaZ9S1Bw6UbZkDLowuuTCjBWUax0vBMMxdy6XjEEK4Oq9lKMvx9JzjmeJf1knoqSNrox3Ka0rnxXpNAz6sATvme8p9mTXyp0cX4lF4U2J54xa2_S9NF5QWvpXvBeC4GAJx7QaSw4zrUkrc6XyaAiFnLhQEwKJCwUw4NOqIuYvYp_IXhw-5Ti_icDlZS-282PcccnBeOcX7vc21pozibIdmZJKqXNsL1Ibx5Nkx1F1jLnekJAmdaACDjYRLL_6n3W4wUp19UvzB1lGtXcJKLLkqB6YDiZNu16OSiSprfmrRXvYmvD8m6Fnl5aetgKw'}"
        }
    }
};
```

The discovery document lists the accepted algorithms in `request_object_signing_alg_values_supported`.

#### NOTE
`Microsoft.IdentityModel.Tokens.JsonWebKeyConverter` has helpers to convert keys to JWKs.

## Passing request JWTs by reference

If the `request_uri` parameter holds a URL, the server makes an outgoing HTTP call to fetch the JWT from it.

This is off by default. Turn it on with `OidcServerOptions.Endpoints.EnableJwtRequestUri` (see
[server options](../reference/options.md)), and register each URL in the client's `RequestUris`: the server only
fetches registered URLs (matched exactly, ignoring a fragment) so it cannot be used to fetch arbitrary ones. Also see
the security considerations of the [specification](https://www.rfc-editor.org/rfc/rfc9101#section-10.4).

```csharp
builder.Services.AddOidcServer(options => options.Endpoints.EnableJwtRequestUri = true);

var client = new Client
{
    ClientId = "foo",
    RequestUris = { "https://foo.example.com/request.jwt" },
    // ...
};
```

A `request_uri` issued by the [pushed authorization endpoint](par.md) (`urn:ietf:params:oauth:request_uri:...`) is
not fetched and does not need this option.

The HTTP client does not follow redirects and does not connect to private or reserved addresses. You can customize
it, e.g. to change the timeout or to add resilience
(`AddStandardResilienceHandler` comes from Microsoft's `Microsoft.Extensions.Http.Resilience` package):

```csharp
builder.Services.AddOidcServer()
    .AddJwtRequestUriHttpClient(client => client.Timeout = TimeSpan.FromSeconds(30))
    .AddStandardResilienceHandler();
```

## Accessing the request object data

You can access the validated data from the request object in two ways:

* wherever you have access to the `ValidatedAuthorizeRequest`, the `RequestObjectValues` dictionary holds the values;
* in the login and consent pages, `IOidcServerInteractionService.GetAuthorizationContextAsync` returns an
  `AuthorizationRequest` that has the `RequestObjectValues` dictionary as well.

[[go back to index]](../index.md#documentation-structure)
