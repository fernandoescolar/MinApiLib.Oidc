[[go back to index]](../index.md#documentation-structure)

# Device Authorization Endpoint

The device authorization endpoint starts the device flow ([RFC 8628](https://www.rfc-editor.org/rfc/rfc8628)): a
device without a browser, or with a limited keyboard, gets a device code for itself and a user code that the user
enters on another device. The device then polls the [token endpoint](token.md) with the device code.

| Route | Parameters |
|---|---|
| `POST /connect/deviceauthorization` | In an `application/x-www-form-urlencoded` body. Any other content type gets `400` `invalid_request` |

With mutual TLS in path mode, the endpoint also answers at `/connect/mtls/deviceauthorization`. Any other HTTP method
gets `405` with an `Allow` header. The endpoint answers `404` when
`OidcServerOptions.Endpoints.EnableDeviceAuthorizationEndpoint` is `false`. Its URL is in the
[discovery document](discovery.md) as `device_authorization_endpoint`.

The client must allow the `urn:ietf:params:oauth:grant-type:device_code` grant type (`GrantTypes.DeviceFlow`). It
authenticates as it would at the token endpoint; public clients send only `client_id`.

## Parameters

`client_id`
: client identifier, in the body or in the `Authorization` header (required).

`client_secret`
: client secret, in the body or in the `Authorization` header, for confidential clients.

`scope`
: one or more registered scopes. Without it, the client gets all its allowed scopes.

## Example

```http
POST /connect/deviceauthorization HTTP/1.1
Host: login.example.com
Content-Type: application/x-www-form-urlencoded

client_id=tv&scope=openid%20profile%20orders.read
```

```http
HTTP/1.1 200 OK
Content-Type: application/json

{
  "device_code": "9F1C2B4E7A6D8C3B0E5F4A1D2C3B4A59",
  "user_code": "482719305",
  "verification_uri": "https://login.example.com/device",
  "verification_uri_complete": "https://login.example.com/device?userCode=482719305",
  "expires_in": 300,
  "interval": 5
}
```

* `verification_uri` comes from `UserInteraction.DeviceVerificationUrl` (`/device`) and the user code parameter from
  `UserInteraction.DeviceVerificationUserCodeParameter` (`userCode`). The host writes that page, with
  `IDeviceFlowInteractionService` (see the [device flow interaction service](../reference/deviceflow_interactionservice.md)).
* `expires_in` is `Client.DeviceCodeLifetime` (300 seconds) and `interval` is `DeviceFlow.Interval` (5 seconds).
* The user code is numeric by default (`DeviceFlow.DefaultUserCodeType`, or `Client.UserCodeType` per client).

The device then polls the token endpoint, no more often than `interval`:

```http
POST /connect/token HTTP/1.1
Host: login.example.com
Content-Type: application/x-www-form-urlencoded

grant_type=urn%3Aietf%3Aparams%3Aoauth%3Agrant-type%3Adevice_code
&client_id=tv
&device_code=9F1C2B4E7A6D8C3B0E5F4A1D2C3B4A59
```

Until the user approves, it gets `authorization_pending` (or `slow_down` if it polls too fast), and then the tokens.

Errors are `400 Bad Request` with a JSON body (`error` and, when there is one, `error_description`), including client
authentication failures (`invalid_client`).

[[go back to index]](../index.md#documentation-structure)
