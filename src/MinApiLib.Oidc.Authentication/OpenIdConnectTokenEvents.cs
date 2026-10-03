namespace MinApiLib.Oidc.Authentication;

/// <summary>
/// Adds to the events of an OpenID Connect scheme what the user tokens need. The events the application set keep
/// running first.
/// </summary>
internal static class OpenIdConnectTokenEvents
{
    public static void Attach(OpenIdConnectOptions options, string scheme, OpenIdConnectTokenOptions settings)
    {
        if (options.EventsType != null)
        {
            throw new InvalidOperationException(
                $"The OpenID Connect scheme '{scheme}' sets EventsType, which AddOpenIdConnectTokens cannot extend. Set the events with options.Events instead.");
        }

        // Keeps access_token, refresh_token, token_type and expires_at in the session.
        options.SaveTokens = true;

        var events = options.Events;

        var onTicketReceived = events.OnTicketReceived;
        events.OnTicketReceived = context =>
        {
            context.Properties!.Items[OpenIdConnectTokenDefaults.SchemeProperty] = scheme;
            return onTicketReceived(context);
        };

        if (!settings.UseDPoP)
        {
            return;
        }

        // The key of the sign-in is created with the authorization request, so the code can be bound to it. It
        // travels in the protected state and ends up, still protected, in the session.
        var onRedirect = events.OnRedirectToIdentityProvider;
        events.OnRedirectToIdentityProvider = async context =>
        {
            await onRedirect(context);
            using var key = settings.CreateDPoPKey();
            context.Properties.Items[OpenIdConnectTokenDefaults.DPoPKeyProperty] = key.ToJwk();
            if (settings.BindAuthorizationCode)
            {
                context.ProtocolMessage.SetParameter("dpop_jkt", key.Thumbprint);
            }
        };

        var onCodeReceived = events.OnAuthorizationCodeReceived;
        events.OnAuthorizationCodeReceived = async context =>
        {
            await onCodeReceived(context);
            if (context.Result == null && !context.HandledCodeRedemption)
            {
                await RedeemWithProofAsync(context);
            }
        };
    }

    /// <summary>Redeems the code like the handler would, with a DPoP proof of the key of the sign-in.</summary>
    private static async Task RedeemWithProofAsync(AuthorizationCodeReceivedContext context)
    {
        if (context.Properties?.Items.TryGetValue(OpenIdConnectTokenDefaults.DPoPKeyProperty, out var jwk) != true || jwk == null)
        {
            context.Fail("The DPoP key of the sign-in is missing from its state.");
            return;
        }

        var cancellationToken = context.HttpContext.RequestAborted;
        var configuration = context.Options.Configuration
            ?? await context.Options.ConfigurationManager!.GetConfigurationAsync(cancellationToken);
        var tokenEndpoint = context.HttpContext.RequestServices.GetRequiredService<TokenEndpointClient>();

        using var key = DPoPKey.FromJwk(jwk);
        var response = await tokenEndpoint.RequestAsync(
            context.Backchannel,
            new Uri(configuration.TokenEndpoint),
            context.TokenEndpointRequest!.Parameters,
            key,
            cancellationToken);

        if (response.IsError)
        {
            context.Fail(new OpenIdConnectProtocolException(response.ToException().Message));
            return;
        }

        context.HandleCodeRedemption(new OpenIdConnectMessage(response.Json));
    }
}
