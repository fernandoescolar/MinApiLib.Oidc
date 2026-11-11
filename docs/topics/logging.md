[[go back to index]](../index.md#documentation-structure)

# Logging

The server logs through the standard .NET logging abstractions (`ILogger<T>`). The Microsoft
[documentation](https://learn.microsoft.com/aspnet/core/fundamentals/logging/) introduces them and the built-in
logging providers.

Every message is a source-generated `[LoggerMessage]` method with a fixed event ID, so a message whose level is off
costs almost nothing, and each message can be found and filtered by its ID. The logger categories are the names of the
server's types, all under the `MinApiLib.Oidc.Server` namespace; startup checks use the
`MinApiLib.Oidc.Server.Startup` category.

The log levels follow the Microsoft guidelines:

* `Trace`: information that is valuable only to a developer troubleshooting an issue. These messages include the
  tokens the server issues and other sensitive data: never enable them in production.
* `Debug`: the internal flow and why decisions are made. Useful during development and debugging.
* `Information`: the general flow of the application, with some long-term value.
* `Warning`: abnormal or unexpected events that do not stop the application but may need investigating, for example
  the in-memory grant store or the developer signing credential outside the Development environment.
* `Error`: errors and exceptions that cannot be handled, for example a failed validation of a protocol request.
* `Critical`: failures that require immediate attention, for example a missing store or an unhandled exception.

## Configuring the levels

Set the levels of the server's categories like any other, for example in `appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "MinApiLib.Oidc.Server": "Debug"
    }
  }
}
```

## Sensitive values

When the server logs a token or authorize request, it leaves out the values listed in `OidcServerOptions.Logging`:

* `TokenRequestSensitiveValuesFilter`: `client_secret`, `password`, `client_assertion`, `refresh_token`,
  `device_code`, `code` and `code_verifier` by default;
* `AuthorizeRequestSensitiveValuesFilter`: `id_token_hint` and `login_hint_token` (which carries the authorization nonce) by default.

Add the names of your own sensitive parameters to them.

## Setup in code

`WebApplication.CreateBuilder` already adds the console, debug and EventSource providers, so the server's messages
reach the console with no setup. `builder.Logging` sets the format and the levels in code:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddSimpleConsole(options =>
{
    options.SingleLine = true;
    options.TimestampFormat = "HH:mm:ss ";
});
builder.Logging.AddFilter("Microsoft", LogLevel.Warning);
builder.Logging.AddFilter("Microsoft.Hosting.Lifetime", LogLevel.Information);
builder.Logging.AddFilter("MinApiLib.Oidc.Server", LogLevel.Debug);
```

For structured output, for example to collect the logs from a container, use `AddJsonConsole()` instead of
`AddSimpleConsole`: each message is a JSON object with its category, event ID and parameters.

Higher level, structured information about what the server does is available as [events](events.md#refevents).

[[go back to index]](../index.md#documentation-structure)
