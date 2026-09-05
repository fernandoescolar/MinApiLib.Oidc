[[go back to index]](../index.md#documentation-structure)

# Nonce Endpoint

The nonce endpoint is used to create a one-time token, the nonce, which can be used to authenticate with a target application. The nonce is a short-lived token that can only be used once. It is safe to use in a URL because it is a number that can only be used once, and is very short lived. So even if a nonce is revealed in the browser history or server logs, it cannot be exploited.

## Example

```csharp
POST /connect/nonce
Authorization: Basic xxxyyy

token=<token>
```

A successful response will return a status code of 200, and the nonce and its expiration time:

```json
{
    "nonce": "abc123...",
    "expires_in": 100
}
```

An invalid request will return a 400, an unauthorized request 401.

#### NOTE

You can read more about the **Nonce Authorization Pattern** [here](../reference/nonce_pattern.md).

[[go back to index]](../index.md#documentation-structure)