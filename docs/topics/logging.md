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

## Setup for Serilog

Any logging provider works. For example, with the third-party [Serilog](https://serilog.net/) and its
`Serilog.AspNetCore` package:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSerilog(logger => logger
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
    .MinimumLevel.Override("Microsoft.AspNetCore.Authentication", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level}] {SourceContext}{NewLine}{Message:lj}{NewLine}{Exception}{NewLine}"));
```

Higher level, structured information about what the server does is available as [events](events.md#refevents).

[[go back to index]](../index.md#documentation-structure)
