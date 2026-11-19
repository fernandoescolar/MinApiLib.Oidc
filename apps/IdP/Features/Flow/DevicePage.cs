namespace IdP.Features.Flow;

/// <summary>
/// The device flow (RFC 8628) verification page: the user types the code the device shows, or arrives with it in
/// <c>user_code</c>, then grants or denies the device's request as on the consent page.
/// </summary>
public sealed record DevicePage() : GetHandlerAsync<DevicePage.Request>("/device")
{
    public readonly record struct Request(
        HttpContext HttpContext,
        [FromQuery(Name = "user_code")] string? UserCode,
        [FromServices] IDeviceFlowInteractionService Devices,
        [FromServices] IAntiforgery Antiforgery,
        [FromServices] IOptions<IdPOptions> Options,
        [FromServices] SpaPages Spa);

    protected override async Task<IResult> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var http = request.HttpContext;
        if (http.User.Identity?.IsAuthenticated != true)
        {
            return FlowPages.RedirectToLogin(http.Request.Path + http.Request.QueryString);
        }

        if (string.IsNullOrWhiteSpace(request.UserCode))
        {
            return RenderCodeForm(http, request.Spa, request.Antiforgery, request.Options.Value, error: null);
        }

        var code = Normalize(request.UserCode);
        var context = await request.Devices.GetAuthorizationContextAsync(code);
        if (context == null)
        {
            return RenderCodeForm(http, request.Spa, request.Antiforgery, request.Options.Value, "That code is not valid or has expired.");
        }

        return ConsentPage.Render(http, request.Spa, request.Antiforgery, request.Options.Value, returnUrl: string.Empty, context.Client, context.ValidatedResources, error: null, userCode: code);
    }

    public static string Normalize(string userCode) => userCode.Replace("-", string.Empty, StringComparison.Ordinal).Replace(" ", string.Empty, StringComparison.Ordinal).ToUpperInvariant();

    public static IResult RenderCodeForm(HttpContext http, SpaPages spa, IAntiforgery antiforgery, IdPOptions options, string? error)
        => spa.Render("device", "Connect a device", new
        {
            error,
            antiforgery = AntiforgeryModel.For(http, antiforgery),
            product = FlowPages.Product(options)
        });
}

/// <summary>The user typed the code: show the device's request.</summary>
public sealed record DeviceCodeForm() : PostHandler<DeviceCodeForm.Request>("/device")
{
    public readonly record struct Request([FromForm] string? UserCode);

    protected override IResult Handle(Request request)
        => Results.Redirect("/device?user_code=" + Uri.EscapeDataString(DevicePage.Normalize(request.UserCode ?? string.Empty)));
}

/// <summary>The user granted or denied the device's request.</summary>
public sealed record DeviceConsentForm() : PostHandlerAsync<DeviceConsentForm.Request>("/device/consent")
{
    public readonly record struct Request(
        HttpContext HttpContext,
        [FromForm] string? UserCode,
        [FromForm] string? Button,
        [FromForm] bool? Remember,
        IFormCollection Form,
        [FromServices] IDeviceFlowInteractionService Devices,
        [FromServices] IAntiforgery Antiforgery,
        [FromServices] IOptions<IdPOptions> Options,
        [FromServices] SpaPages Spa);

    protected override async Task<IResult> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var http = request.HttpContext;
        var options = request.Options.Value;
        if (http.User.Identity?.IsAuthenticated != true)
        {
            return FlowPages.RedirectToLogin("/device");
        }

        var code = DevicePage.Normalize(request.UserCode ?? string.Empty);
        var context = await request.Devices.GetAuthorizationContextAsync(code);
        if (context == null)
        {
            return DevicePage.RenderCodeForm(http, request.Spa, request.Antiforgery, options, "That code is not valid or has expired.");
        }

        ConsentResponse consent;
        if (request.Button != "allow")
        {
            consent = new ConsentResponse { Error = AuthorizationError.AccessDenied };
        }
        else
        {
            var granted = Consent.Build(context.ValidatedResources, request.Form["scopes"].OfType<string>().ToArray(), request.Remember == true);
            if (granted == null)
            {
                return ConsentPage.Render(http, request.Spa, request.Antiforgery, options, string.Empty, context.Client, context.ValidatedResources, "Choose at least one permission, or deny the request.", code);
            }

            consent = granted;
        }

        var result = await request.Devices.HandleRequestAsync(code, consent);
        if (result.IsError)
        {
            return DevicePage.RenderCodeForm(http, request.Spa, request.Antiforgery, options, result.ErrorDescription ?? "The device could not be authorized.");
        }

        var name = context.Client.ClientName ?? context.Client.ClientId;
        return result.IsAccessDenied
            ? FlowPages.Message(request.Spa, options, "Request denied", $"{name} was not given access. You can close this page.", "/account", "Go to my account")
            : FlowPages.Message(request.Spa, options, "Device connected", $"{name} now has access. You can close this page and go back to the device.", "/account", "Go to my account");
    }
}
