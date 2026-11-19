namespace IdP.Features.Flow;

// The server side of the passkey sign-in, with ASP.NET Core Identity's IPasskeyHandler: it makes the options for the
// browser and checks the browser's answer (signature, challenge, origin, counter). The ceremony state travels in an
// encrypted cookie (PasskeyState). The form-bound endpoints validate the antiforgery token.

/// <summary>Options to sign in with any passkey of this server (a discoverable credential).</summary>
public sealed record PasskeyRequestOptions() : PostHandlerAsync<PasskeyRequestOptions.Request>("/login/passkey/options")
{
    public readonly record struct Request(
        HttpContext HttpContext,
        IFormCollection Form,
        [FromServices] IPasskeyHandler<IdPUser> Passkeys,
        [FromServices] PasskeyState State);

    protected override async Task<IResult> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var options = await request.Passkeys.MakeRequestOptionsAsync(null, request.HttpContext);
        request.State.Save(request.HttpContext, PasskeyState.Assertion, options.AssertionState ?? throw new InvalidOperationException("No assertion state."));
        return Results.Content(options.RequestOptionsJson, "application/json");
    }
}

/// <summary>Checks the passkey the browser used, signs its user in and returns to the authorize request.</summary>
public sealed record PasskeyLoginForm() : PostHandlerAsync<PasskeyLoginForm.Request>("/login/passkey")
{
    public readonly record struct Request(
        HttpContext HttpContext,
        [FromForm] string? Credential,
        [FromForm] string? ReturnUrl,
        [FromServices] IPasskeyHandler<IdPUser> Passkeys,
        [FromServices] UserManager<IdPUser> Users,
        [FromServices] IdPMongoContext Db,
        [FromServices] IEventService Events,
        [FromServices] PasskeyState State,
        [FromServices] IOidcServerInteractionService Interaction,
        [FromServices] LoginRenderer Renderer);

    protected override async Task<IResult> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var http = request.HttpContext;
        var context = request.ReturnUrl == null ? null : await request.Interaction.GetAuthorizationContextAsync(request.ReturnUrl);
        var state = request.State.Take(http, PasskeyState.Assertion);
        if (state == null || string.IsNullOrEmpty(request.Credential))
        {
            return await request.Renderer.RenderAsync(http, request.ReturnUrl, context, "The passkey sign-in expired. Try again.");
        }

        var result = await request.Passkeys.PerformAssertionAsync(new PasskeyAssertionContext
        {
            HttpContext = http,
            CredentialJson = request.Credential,
            AssertionState = state
        });

        if (!result.Succeeded)
        {
            await request.Events.RaiseAsync(new UserLoginFailureEvent("(passkey)", result.Failure.Message, clientId: context?.Client.ClientId));
            return await request.Renderer.RenderAsync(http, request.ReturnUrl, context, $"The passkey was not accepted: {result.Failure.Message}");
        }

        if (!result.User.Enabled || await request.Users.IsLockedOutAsync(result.User))
        {
            return await request.Renderer.RenderAsync(http, request.ReturnUrl, context, "This account cannot sign in right now.");
        }

        // Keep the new signature counter, which reveals cloned authenticators.
        await request.Users.AddOrUpdatePasskeyAsync(result.User, result.Passkey);
        await SignIn.WithPasskeyAsync(http, result.User, result.Passkey, request.Db, request.Events);
        return SignIn.Return(request.ReturnUrl, context);
    }
}
