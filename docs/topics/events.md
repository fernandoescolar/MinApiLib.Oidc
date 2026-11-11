[[go back to index]](../index.md#documentation-structure)

<a id="refevents"></a>

# Events

While logging is low level, "printf" style, events represent higher level information about operations of the
server. Events are structured data with an ID, a name, a category, a type (success, failure, information or error)
and details. This makes them easy to query and analyze in event stores like
[Elasticsearch](https://www.elastic.co/elastic-stack), [Seq](https://datalust.co/seq) or
[Splunk](https://www.splunk.com/).

The types live in `MinApiLib.Oidc.Server.Core.Events`; each built-in event lives in the namespace of the feature that
raises it (for example `UserLoginSuccessEvent` in `MinApiLib.Oidc.Server.Features.Interaction`).

## Emitting events

Events are off by default. Turn them on with `OidcServerOptions.Events`:

```csharp
builder.Services.AddOidcServer(options =>
{
    options.Events.RaiseSuccessEvents = true;
    options.Events.RaiseFailureEvents = true;
    options.Events.RaiseErrorEvents = true;
    options.Events.RaiseInformationEvents = true;
});
```

The server raises its events itself. To raise one from your own code, for example from your login page, call
`RaiseAsync` on the `IEventService` from the container:

```csharp
if (await users.CheckPasswordAsync(user, password))
{
    await events.RaiseAsync(new UserLoginSuccessEvent(user.UserName!, user.Id, user.UserName!, clientId: context?.Client.ClientId));
}
else
{
    await events.RaiseAsync(new UserLoginFailureEvent(userName, "invalid credentials", clientId: context?.Client.ClientId));
}
```

Here `users` is ASP.NET Core Identity's `UserManager<TUser>`, `events` the `IEventService` and `context` the
`AuthorizationRequest` from `IOidcServerInteractionService.GetAuthorizationContextAsync(returnUrl)`.
`IEventService.CanRaiseEventType` says whether events of a type are turned on.

## Custom sinks

The default sink, `DefaultEventSink`, serializes the event to JSON and writes it to the log at the `Information`
level. To send events to an event store, implement `IEventSink` and register it in the container. Register it after
`AddOidcServer`, or before it: either way it replaces the default (see [Startup](startup.md#refstartup)).

The following example writes the events to their own log category with the .NET logger, as warnings when they are
failures or errors, with the event's ID as the log event ID:

```csharp
public sealed class AuditEventSink(ILogger<AuditEventSink> logger) : IEventSink
{
    public Task PersistAsync(Event evt)
    {
        var level = evt.EventType is EventTypes.Success or EventTypes.Information
            ? LogLevel.Information
            : LogLevel.Warning;

        // Event.ToString() serializes the event to JSON.
        logger.Log(level, new EventId(evt.Id, evt.Name), "{EventName}: {Details}", evt.Name, evt.ToString());

        return Task.CompletedTask;
    }
}
```

```csharp
builder.Services.AddSingleton<IEventSink, AuditEventSink>();
```

The category, the sink's full type name, can then get its own level or provider in the logging configuration (see
[Logging](logging.md)).

## Built-in events

The server raises these events:

`ApiAuthenticationSuccessEvent` & `ApiAuthenticationFailureEvent`
: Successful/failed API authentication at the introspection endpoint.

`ClientAuthenticationSuccessEvent` & `ClientAuthenticationFailureEvent`
: Successful/failed client authentication, at the token endpoint and the other endpoints that authenticate clients.

`TokenIssuedSuccessEvent` & `TokenIssuedFailureEvent`
: Successful/failed requests for identity tokens, access tokens, refresh tokens and authorization codes, at the token
  and authorize endpoints.

`TokenIntrospectionSuccessEvent` & `TokenIntrospectionFailureEvent`
: Successful/failed token introspection requests.

`TokenRevokedSuccessEvent`
: Successful token revocation requests.

`DeviceAuthorizationSuccessEvent` & `DeviceAuthorizationFailureEvent`
: Successful/failed device authorization requests.

`NonceIssuedSuccessEvent` & `NonceIssuedFailureEvent`
: Successful/failed requests at the nonce endpoint.

`UserLoginSuccessEvent` & `UserLoginFailureEvent`
: Raised by the resource owner password grant (with `interactive: false`). Raise them from your login page too: the
  server does not see the credentials.

`UserLogoutSuccessEvent`
: A signed-in user signed out of the server's cookie scheme, with `HttpContext.SignOutAsync()` on your logout page
  or when the nonce pattern replaces the signed-in user.

`ConsentGrantedEvent` & `ConsentDeniedEvent`
: The user granted or denied consent: at the authorize endpoint, when the server processes the response your consent
  page gave to `IOidcServerInteractionService.GrantConsentAsync` (leaving out a required scope is a denial), and in
  the device flow, in `IDeviceFlowInteractionService.HandleRequestAsync`.

`GrantsRevokedEvent`
: The user revoked the grants of a client with `IOidcServerInteractionService.RevokeUserConsentAsync`.

`InvalidClientConfigurationEvent`
: A client loaded from the client store failed configuration validation.

`UnhandledExceptionEvent`
: An unhandled exception in a protocol endpoint.

## Custom events

You can create your own events and raise them through the same infrastructure.

Derive from the base `Event` class, which adds contextual information like the activity ID, the timestamp, the
process ID and the IP addresses. Your derived class can then add fields specific to the event. Pick an ID that does
not collide with `EventIds`:

```csharp
public class PasswordChangedEvent : Event
{
    public PasswordChangedEvent(string subjectId)
        : base(EventCategories.Authentication,
               "Password Changed",
               EventTypes.Information,
               id: 9000)
    {
        SubjectId = subjectId;
    }

    public string SubjectId { get; set; }
}
```

[[go back to index]](../index.md#documentation-structure)
