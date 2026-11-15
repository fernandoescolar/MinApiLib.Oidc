namespace Samples.Server.Pages;

// The server side of passkeys, with ASP.NET Core Identity's IPasskeyHandler: it makes the options for the browser
// and checks the browser's answer (signature, challenge, origin, counter). The ceremony state travels in an
// encrypted cookie (PasskeyState). The form-bound endpoints validate the antiforgery token.

/// <summary>Options to sign in with any passkey of this server (a discoverable credential).</summary>
public sealed record PasskeyRequestOptions() : PostHandlerAsync<PasskeyRequestOptions.Request>("/account/passkey/request-options")
{
    public readonly record struct Request(
        HttpContext HttpContext,
        IFormCollection Form,
        [FromServices] IPasskeyHandler<SampleUser> Passkeys,
        [FromServices] PasskeyState State);

    protected override async Task<IResult> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var options = await request.Passkeys.MakeRequestOptionsAsync(null, request.HttpContext);
        request.State.Save(request.HttpContext, PasskeyState.Assertion, options.AssertionState ?? throw new InvalidOperationException("No assertion state."));
        return Results.Content(options.RequestOptionsJson, "application/json");
    }
}

/// <summary>Checks the passkey the browser used, signs its user in and returns to the authorize request.</summary>
public sealed record PasskeyLoginForm() : PostHandlerAsync<PasskeyLoginForm.Request>("/account/passkey/login")
{
    public readonly record struct Request(
        HttpContext HttpContext,
        [FromForm] string? Credential,
        [FromForm] string? ReturnUrl,
        [FromServices] IPasskeyHandler<SampleUser> Passkeys,
        [FromServices] UserManager<SampleUser> Users,
        [FromServices] PasskeyState State,
        [FromServices] IOidcServerInteractionService Interaction,
        [FromServices] IAntiforgery Antiforgery,
        [FromServices] SpaPages Spa);

    protected override async Task<IResult> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var http = request.HttpContext;
        var context = request.ReturnUrl == null ? null : await request.Interaction.GetAuthorizationContextAsync(request.ReturnUrl);
        var state = request.State.Take(http, PasskeyState.Assertion);
        if (state == null || string.IsNullOrEmpty(request.Credential))
        {
            return LoginPage.Render(http, request.Spa, request.Antiforgery, request.ReturnUrl, context, "The passkey sign-in expired. Try again.");
        }

        var result = await request.Passkeys.PerformAssertionAsync(new PasskeyAssertionContext
        {
            HttpContext = http,
            CredentialJson = request.Credential,
            AssertionState = state
        });

        if (!result.Succeeded)
        {
            return LoginPage.Render(http, request.Spa, request.Antiforgery, request.ReturnUrl, context, $"The passkey was not accepted: {result.Failure.Message}");
        }

        // Keep the new signature counter, which reveals cloned authenticators.
        await request.Users.AddOrUpdatePasskeyAsync(result.User, result.Passkey);
        await Login.SignInWithPasskeyAsync(http, result.User, result.Passkey);
        return Login.Return(request.ReturnUrl, context);
    }
}

/// <summary>Options to create a passkey for the signed-in user.</summary>
public sealed record PasskeyCreationOptions() : PostHandlerAsync<PasskeyCreationOptions.Request>("/account/passkey/creation-options")
{
    public readonly record struct Request(
        HttpContext HttpContext,
        IFormCollection Form,
        [FromServices] IPasskeyHandler<SampleUser> Passkeys,
        [FromServices] UserManager<SampleUser> Users,
        [FromServices] PasskeyState State);

    protected override async Task<IResult> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var user = await PasskeysPage.CurrentUserAsync(request.HttpContext, request.Users);
        if (user == null)
        {
            return Results.Unauthorized();
        }

        // The options exclude the user's passkeys, so an authenticator does not get a second one.
        var options = await request.Passkeys.MakeCreationOptionsAsync(
            new PasskeyUserEntity { Id = user.Id, Name = user.UserName, DisplayName = user.Name },
            request.HttpContext);
        request.State.Save(request.HttpContext, PasskeyState.Attestation, options.AttestationState ?? throw new InvalidOperationException("No attestation state."));
        return Results.Content(options.CreationOptionsJson, "application/json");
    }
}

/// <summary>Checks the new passkey and adds it to the signed-in user.</summary>
public sealed record PasskeyRegisterForm() : PostHandlerAsync<PasskeyRegisterForm.Request>("/account/passkey/register")
{
    public readonly record struct Request(
        HttpContext HttpContext,
        [FromForm] string? Credential,
        [FromForm] string? Name,
        [FromServices] IPasskeyHandler<SampleUser> Passkeys,
        [FromServices] UserManager<SampleUser> Users,
        [FromServices] PasskeyState State);

    protected override async Task<IResult> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var http = request.HttpContext;
        var user = await PasskeysPage.CurrentUserAsync(http, request.Users);
        if (user == null)
        {
            return Results.Redirect("/account/passkeys");
        }

        var state = request.State.Take(http, PasskeyState.Attestation);
        if (state == null || string.IsNullOrEmpty(request.Credential))
        {
            return PasskeysPage.Redirect("The passkey registration expired. Try again.");
        }

        var result = await request.Passkeys.PerformAttestationAsync(new PasskeyAttestationContext
        {
            HttpContext = http,
            CredentialJson = request.Credential,
            AttestationState = state
        });

        if (!result.Succeeded)
        {
            return PasskeysPage.Redirect($"The passkey was not accepted: {result.Failure.Message}");
        }

        if (result.UserEntity.Id != user.Id)
        {
            return PasskeysPage.Redirect("The passkey was created for another user.");
        }

        result.Passkey.Name = string.IsNullOrWhiteSpace(request.Name) ? $"Passkey of {result.Passkey.CreatedAt:yyyy-MM-dd HH:mm}" : request.Name.Trim();
        await request.Users.AddOrUpdatePasskeyAsync(user, result.Passkey);
        return PasskeysPage.Redirect(message: "Passkey added. Sign out and sign in with it.");
    }
}

/// <summary>Removes a passkey of the signed-in user.</summary>
public sealed record PasskeyRemoveForm() : PostHandlerAsync<PasskeyRemoveForm.Request>("/account/passkey/remove")
{
    public readonly record struct Request(
        HttpContext HttpContext,
        [FromForm] string? Id,
        [FromServices] UserManager<SampleUser> Users);

    protected override async Task<IResult> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var user = await PasskeysPage.CurrentUserAsync(request.HttpContext, request.Users);
        if (user != null && request.Id != null)
        {
            await request.Users.RemovePasskeyAsync(user, Base64Url.DecodeFromChars(request.Id));
        }

        return PasskeysPage.Redirect(message: "Passkey removed.");
    }
}
