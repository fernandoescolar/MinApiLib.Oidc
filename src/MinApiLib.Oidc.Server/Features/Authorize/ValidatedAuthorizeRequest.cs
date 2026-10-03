namespace MinApiLib.Oidc.Server.Features.Authorize;

/// <summary>
/// Models a validated request to the authorize endpoint.
/// </summary>
public class ValidatedAuthorizeRequest : ValidatedRequest
{
    /// <summary>
    /// Gets or sets the type of the response.
    /// </summary>
    /// <value>
    /// The type of the response.
    /// </value>
    public string ResponseType { get; set; } = default!;

    /// <summary>
    /// Gets or sets the response mode.
    /// </summary>
    /// <value>
    /// The response mode.
    /// </value>
    public string ResponseMode { get; set; } = default!;

    /// <summary>
    /// Gets or sets the grant type.
    /// </summary>
    /// <value>
    /// The grant type.
    /// </value>
    public string GrantType { get; set; } = default!;

    /// <summary>
    /// Gets or sets the redirect URI.
    /// </summary>
    /// <value>
    /// The redirect URI.
    /// </value>
    public string RedirectUri { get; set; } = default!;

    /// <summary>
    /// Gets or sets the requested scopes.
    /// </summary>
    /// <value>
    /// The requested scopes.
    /// </value>
    public List<string> RequestedScopes { get; set; }

    /// <summary>
    /// Gets or sets the resource indicators (RFC 8707) requested by the client.
    /// </summary>
    public IEnumerable<string>? RequestedResourceIndicators { get; set; }

    /// <summary>
    /// Gets or sets the JWK thumbprint the authorization code is bound to (<c>dpop_jkt</c>, RFC 9449 section 10).
    /// </summary>
    public string? DPoPKeyThumbprint { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether consent was shown.
    /// </summary>
    /// <value>
    ///   <c>true</c> if consent was shown; otherwise, <c>false</c>.
    /// </value>
    public bool WasConsentShown { get; set; }

    /// <summary>
    /// Gets or sets the description the user assigned to this grant, for example the name of the device or application.
    /// </summary>
    /// <value>
    /// The description.
    /// </value>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the state.
    /// </summary>
    /// <value>
    /// The state.
    /// </value>
    public string? State { get; set; }

    /// <summary>
    /// Gets or sets the UI locales.
    /// </summary>
    /// <value>
    /// The UI locales.
    /// </value>
    public string? UiLocales { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the request was an OpenID Connect request.
    /// </summary>
    /// <value>
    /// <c>true</c> if the request was an OpenID Connect request; otherwise, <c>false</c>.
    /// </value>
    public bool IsOpenIdRequest { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this instance is API resource request.
    /// </summary>
    /// <value>
    /// <c>true</c> if this instance is API resource request; otherwise, <c>false</c>.
    /// </value>
    public bool IsApiResourceRequest { get; set; }

    /// <summary>
    /// Gets or sets the nonce.
    /// </summary>
    /// <value>
    /// The nonce.
    /// </value>
    public string? Nonce { get; set; }

    /// <summary>
    /// Gets or sets the authentication context reference classes.
    /// </summary>
    /// <value>
    /// The authentication context reference classes.
    /// </value>
    public List<string> AuthenticationContextReferenceClasses { get; set; }

    /// <summary>
    /// Gets or sets the display mode.
    /// </summary>
    /// <value>
    /// The display mode.
    /// </value>
    public string? DisplayMode { get; set; }

    /// <summary>
    /// Gets or sets the collection of prompt modes.
    /// </summary>
    /// <value>
    /// The collection of prompt modes.
    /// </value>
    public IEnumerable<string> PromptModes { get; set; } = Enumerable.Empty<string>();

    /// <summary>
    /// Gets or sets the maximum age.
    /// </summary>
    /// <value>
    /// The maximum age.
    /// </value>
    public int? MaxAge { get; set; }

    /// <summary>
    /// Gets or sets the login hint.
    /// </summary>
    /// <value>
    /// The login hint.
    /// </value>
    public string? LoginHint { get; set; }

    /// <summary>
    /// Gets or sets the login hint token.
    /// It is used in the nonce authorization flow pattern.
    /// Nonce authorization requires arc_values and login_hint_token to be present.
    /// </summary>
    public string? LoginHintToken { get; set; }

    /// <summary>
    /// Gets or sets the code challenge
    /// </summary>
    /// <value>
    /// The code challenge
    /// </value>
    public string? CodeChallenge { get; set; }

    /// <summary>
    /// Gets or sets the code challenge method
    /// </summary>
    /// <value>
    /// The code challenge method
    /// </value>
    public string? CodeChallengeMethod { get; set; }

    /// <summary>
    /// Gets or sets the validated contents of the request object (if present)
    /// </summary>
    /// <value>
    /// The request object values
    /// </value>
    public Dictionary<string, string> RequestObjectValues { get; set; } = [];

    /// <summary>
    /// Gets or sets the reference value of the pushed authorization request (RFC 9126) the parameters came from,
    /// or <c>null</c> when the request was not pushed.
    /// </summary>
    public string? PushedAuthorizationReferenceValue { get; set; }

    /// <summary>
    /// Gets or sets the pushed authorization request (RFC 9126) the parameters came from.
    /// </summary>
    public PushedAuthorizationRequest? PushedAuthorizationRequest { get; set; }

    /// <summary>
    /// Gets or sets the request object (either passed by value or retrieved by reference)
    /// </summary>
    /// <value>
    /// The request object
    /// </value>
    public string? RequestObject { get; set; }

    /// <summary>
    /// Gets a value indicating whether an access token was requested.
    /// </summary>
    /// <value>
    /// <c>true</c> if an access token was requested; otherwise, <c>false</c>.
    /// </value>
    public bool AccessTokenRequested => ResponseType is OidcConstants.ResponseTypes.IdTokenToken or
                                        OidcConstants.ResponseTypes.Code or
                                        OidcConstants.ResponseTypes.CodeIdToken;

    public bool IsNoncePatternFlow => AuthenticationContextReferenceClasses.Contains(NonceAuthorization.AcrValue);


    /// <summary>
    /// Initializes a new instance of the <see cref="ValidatedAuthorizeRequest"/> class.
    /// </summary>
    public ValidatedAuthorizeRequest()
    {
        RequestedScopes = [];
        AuthenticationContextReferenceClasses = [];
    }
}
