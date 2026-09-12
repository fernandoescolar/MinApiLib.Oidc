namespace MinApiLib.OidcServer.Features.Authorize;

/// <summary>
/// Default logic for determining if user must login or consent when making requests to the authorization endpoint.
/// </summary>
/// <seealso cref="MinApiLib.OidcServer.Features.Authorize.IAuthorizeInteractionResponseGenerator" />
[RegisterInIServiceCollection(typeof(IAuthorizeInteractionResponseGenerator))]
public class AuthorizeInteractionResponseGenerator : IAuthorizeInteractionResponseGenerator
{
    /// <summary>
    /// The prompt values processed by this class itself. Any other accepted value is handed to the
    /// <see cref="IAuthorizePromptHandler"/> registered for it.
    /// </summary>
    public static readonly IReadOnlySet<string> BuiltInPromptValues = new[]
    {
        OidcConstants.PromptModes.None,
        OidcConstants.PromptModes.Login,
        OidcConstants.PromptModes.Consent,
        OidcConstants.PromptModes.SelectAccount
    }.ToFrozenSet(StringComparer.Ordinal);

    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// The logger.
    /// </summary>
    protected readonly ILogger Logger;

    /// <summary>
    /// The consent service.
    /// </summary>
    protected readonly IConsentService Consent;

    /// <summary>
    /// The profile service.
    /// </summary>
    protected readonly IProfileService Profile;

    /// <summary>
    /// The clock
    /// </summary>
    protected readonly TimeProvider Clock;

    /// <summary>
    /// The nonce store
    /// </summary>
    protected readonly IAuthorizationNonceStore NonceStore;

    /// <summary>
    /// The http context
    /// </summary>
    protected HttpContext HttpContext => _httpContextAccessor.HttpContext!;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthorizeInteractionResponseGenerator"/> class.
    /// </summary>
    /// <param name="clock">The clock.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="consent">The consent.</param>
    /// <param name="profile">The profile.</param>
    /// <param name="nonceStore">The store of nonces issued by the nonce endpoint.</param>
    /// <param name="httpContextAccessor">The HTTP context accessor.</param>
    public AuthorizeInteractionResponseGenerator(
        TimeProvider clock,
        ILogger<AuthorizeInteractionResponseGenerator> logger,
        IConsentService consent,
        IProfileService profile,
        IAuthorizationNonceStore nonceStore,
        IHttpContextAccessor httpContextAccessor)
    {
        Clock = clock;
        Logger = logger;
        Consent = consent;
        Profile = profile;
        NonceStore = nonceStore;
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// Processes the interaction logic.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <param name="consent">The consent.</param>
    /// <returns></returns>
    public virtual async Task<InteractionResponse> ProcessInteractionAsync(ValidatedAuthorizeRequest request, ConsentResponse? consent = null)
    {
        ResponseAndHostingLog.ProcessInteraction(Logger);

        if (consent != null && consent.Granted == false && consent.Error.HasValue && request.Subject.IsAuthenticated() == false)
        {
            ResponseAndHostingLog.UserConsentResultError(Logger, consent.Error);

            var error = consent.Error switch
            {
                AuthorizationError.AccountSelectionRequired => OidcConstants.AuthorizeErrors.AccountSelectionRequired,
                AuthorizationError.ConsentRequired => OidcConstants.AuthorizeErrors.ConsentRequired,
                AuthorizationError.InteractionRequired => OidcConstants.AuthorizeErrors.InteractionRequired,
                AuthorizationError.LoginRequired => OidcConstants.AuthorizeErrors.LoginRequired,
                _ => OidcConstants.AuthorizeErrors.AccessDenied
            };

            return new InteractionResponse
            {
                Error = error,
                ErrorDescription = consent.ErrorDescription
            };
        }


        var result = await ProcessNoncePatternAsync(HttpContext, request);
        if (result is null || !(result.IsError || result.IsLogin || result.IsRedirect))
        {
            result = await ProcessCustomPromptAsync(request);
        }
        if (result is null || !(result.IsError || result.IsLogin || result.IsConsent || result.IsRedirect))
        {
            // A nonce sign-in establishes the session, but the target client's login policies
            // (max_age, IdentityProviderRestrictions, EnableLocalLogin, UserSsoLifetime) still apply.
            result = await ProcessLoginAsync(request);
        }
        if (!result.IsLogin && !result.IsError && !result.IsRedirect && !result.IsConsent)
        {
            result = await ProcessConsentAsync(request, consent);
        }

        if ((result.IsLogin || result.IsConsent || result.IsRedirect) && request.PromptModes.Contains(OidcConstants.PromptModes.None))
        {
            ResponseAndHostingLog.ChangingResponseToLoginRequired(Logger);
            result = new InteractionResponse
            {
                Error = result.IsLogin ? OidcConstants.AuthorizeErrors.LoginRequired :
                    result.IsConsent ? OidcConstants.AuthorizeErrors.ConsentRequired :
                    OidcConstants.AuthorizeErrors.InteractionRequired
            };
        }

        return result;
    }

    /// <summary>
    /// Runs the <see cref="IAuthorizePromptHandler"/> registered for each custom prompt value of the request, in order,
    /// until one needs interaction. Returns <c>null</c> when no handler needs interaction.
    /// </summary>
    /// <param name="request">The request.</param>
    protected internal virtual async Task<InteractionResponse?> ProcessCustomPromptAsync(ValidatedAuthorizeRequest request)
    {
        foreach (var prompt in request.PromptModes.Where(p => !BuiltInPromptValues.Contains(p)).ToList())
        {
            var handler = HttpContext.RequestServices.GetKeyedService<IAuthorizePromptHandler>(prompt);
            if (handler == null)
            {
                ResponseAndHostingLog.NoPromptHandler(Logger, prompt);
                request.RemovePrompt(prompt);
                continue;
            }

            var result = await handler.ProcessAsync(request, prompt);
            if (result.IsError || result.IsLogin || result.IsConsent || result.IsRedirect)
            {
                ResponseAndHostingLog.CustomPromptHandled(Logger, prompt);

                // The page gets the request as return URL; without the prompt it does not trigger the handler again.
                request.RemovePrompt();
                return result;
            }

            // Handled without interaction: the login and consent checks only expect built-in values.
            request.RemovePrompt(prompt);
        }

        return null;
    }

    protected internal virtual async Task<InteractionResponse?> ProcessNoncePatternAsync(HttpContext context, ValidatedAuthorizeRequest request)
    {
        if (!request.IsNoncePatternFlow)
        {
            return default;
        }

        if (request.PromptModes.Any(s => s != OidcConstants.PromptModes.None))
        {
            ResponseAndHostingLog.PromptNotAllowedInNonceFlow(Logger);
            return new InteractionResponse
            {
                Error = OidcConstants.AuthorizeErrors.InvalidRequest,
                ErrorDescription = "Prompt is not allowed in nonce pattern flow."
            };
        }

        var nonce = request.LoginHintToken;
        if (nonce.IsMissing())
        {
            ResponseAndHostingLog.NonceMissing(Logger);
            return new InteractionResponse
            {
                Error = OidcConstants.AuthorizeErrors.InvalidRequest,
                ErrorDescription = "Nonce is missing."
            };
        }

        var authorizationNonce = await NonceStore.GetAuthorizationNonceAsync(nonce);
        if (authorizationNonce is null)
        {
            ResponseAndHostingLog.NonceInvalid(Logger);
            return new InteractionResponse
            {
                Error = OidcConstants.AuthorizeErrors.InvalidRequest,
                ErrorDescription = "Nonce is invalid."
            };
        }

        await NonceStore.RemoveAuthorizationNonceAsync(nonce);

        if (authorizationNonce.CreationTime.HasExceeded(authorizationNonce.Lifetime, Clock.GetUtcNow().UtcDateTime))
        {
            ResponseAndHostingLog.NonceExpired(Logger);
            return new InteractionResponse
            {
                Error = OidcConstants.AuthorizeErrors.InvalidRequest,
                ErrorDescription = "Nonce is invalid."
            };
        }

        var isActiveCtx = new IsActiveContext(authorizationNonce.Subject, request.Client, ProfileIsActiveCallers.AuthorizeEndpoint);
        await Profile.IsActiveAsync(isActiveCtx);
        if (!isActiveCtx.IsActive)
        {
            ResponseAndHostingLog.NonceUserNotActive(Logger);
            return new InteractionResponse
            {
                Error = OidcConstants.AuthorizeErrors.AccessDenied,
                ErrorDescription = "Nonce is invalid."
            };
        }

        var nonceSubject = authorizationNonce.Subject.GetSubjectIdOrNull();
        if (nonceSubject.IsMissing())
        {
            ResponseAndHostingLog.NonceSubjectMissing(Logger);
            return new InteractionResponse
            {
                Error = OidcConstants.AuthorizeErrors.AccessDenied,
                ErrorDescription = "Nonce is invalid."
            };
        }

        if (context.User.IsAuthenticated())
        {
            var currentSubject = context.User.GetSubjectIdOrNull();
            if (currentSubject == nonceSubject)
            {
                ResponseAndHostingLog.UserAlreadyAuthenticatedWithNonceSubject(Logger, nonceSubject);
                return new InteractionResponse();
            }

            ResponseAndHostingLog.UserAuthenticatedWithDifferentSubject(Logger, currentSubject);
            await context.SignOutAsync();
        }

        ResponseAndHostingLog.SigningInWithNonceSubject(Logger, nonceSubject);
        var properties = new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = Clock.GetUtcNow().Add(TimeSpan.FromDays(1))
        };
        await context.SignInAsync(authorizationNonce.Subject, properties);

        // Update the request with the subject from the nonce.
        // This is necessary to ensure that the request is processed with the correct subject.
        request.Subject = authorizationNonce.Subject;

        // The sign-in created a new session; tokens issued for this request must carry its sid.
        var sessionId = properties.GetSessionId();
        if (sessionId.IsPresent())
        {
            request.SessionId = sessionId;
        }

        return new InteractionResponse();
    }

    /// <summary>
    /// Processes the login logic.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <returns></returns>
    protected internal virtual async Task<InteractionResponse> ProcessLoginAsync(ValidatedAuthorizeRequest request)
    {
        if (request.PromptModes.Contains(OidcConstants.PromptModes.Login) ||
            request.PromptModes.Contains(OidcConstants.PromptModes.SelectAccount))
        {
            ResponseAndHostingLog.ShowingLoginPrompt(Logger, request.PromptModes.ToSpaceSeparatedString());

            request.RemovePrompt();

            return new InteractionResponse { IsLogin = true };
        }

        var isAuthenticated = request.Subject.IsAuthenticated();

        bool isActive = false;

        if (isAuthenticated)
        {
            var isActiveCtx = new IsActiveContext(request.Subject, request.Client, OidcServerConstants.ProfileIsActiveCallers.AuthorizeEndpoint);
            await Profile.IsActiveAsync(isActiveCtx);

            isActive = isActiveCtx.IsActive;
        }

        if (!isAuthenticated || !isActive)
        {
            if (!isAuthenticated)
            {
                ResponseAndHostingLog.ShowingLoginNotAuthenticated(Logger);
            }
            else if (!isActive)
            {
                ResponseAndHostingLog.ShowingLoginNotActive(Logger);
            }

            return new InteractionResponse { IsLogin = true };
        }

        var currentIdp = request.Subject.GetIdentityProvider();

        var idp = request.GetIdP();
        if (idp.IsPresent())
        {
            if (idp != currentIdp)
            {
                ResponseAndHostingLog.ShowingLoginIdpMismatch(Logger, currentIdp, idp);
                return new InteractionResponse { IsLogin = true };
            }
        }

        if (request.MaxAge.HasValue)
        {
            var authTime = request.Subject.GetAuthenticationTime();
            if (Clock.GetUtcNow() > authTime.AddSeconds(request.MaxAge.Value))
            {
                ResponseAndHostingLog.ShowingLoginMaxAgeExceeded(Logger);

                return new InteractionResponse { IsLogin = true };
            }
        }

        if (currentIdp == OidcServerConstants.LocalIdentityProvider)
        {
            if (!request.Client.EnableLocalLogin)
            {
                ResponseAndHostingLog.ShowingLoginLocalLoginNotAllowed(Logger);
                return new InteractionResponse { IsLogin = true };
            }
        }
        else if (request.Client.IdentityProviderRestrictions != null &&
                 request.Client.IdentityProviderRestrictions.Count > 0 &&
                 !request.Client.IdentityProviderRestrictions.Contains(currentIdp))
        {
            ResponseAndHostingLog.ShowingLoginIdpRestricted(Logger, currentIdp);
            return new InteractionResponse { IsLogin = true };
        }

        if (request.Client.UserSsoLifetime.HasValue)
        {
            var authTimeEpoch = request.Subject.GetAuthenticationTimeEpoch();
            var nowEpoch = Clock.GetUtcNow().ToUnixTimeSeconds();

            var diff = nowEpoch - authTimeEpoch;
            if (diff > request.Client.UserSsoLifetime.Value)
            {
                ResponseAndHostingLog.ShowingLoginSsoLifetimeExceeded(Logger, diff, request.Client.UserSsoLifetime);
                return new InteractionResponse { IsLogin = true };
            }
        }

        return new InteractionResponse();
    }

    /// <summary>
    /// Processes the consent logic.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <param name="consent">The consent.</param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentException">Invalid PromptMode</exception>
    protected internal virtual async Task<InteractionResponse> ProcessConsentAsync(ValidatedAuthorizeRequest request, ConsentResponse? consent = null)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.PromptModes.Any() &&
            !request.PromptModes.Contains(OidcConstants.PromptModes.None) &&
            !request.PromptModes.Contains(OidcConstants.PromptModes.Consent))
        {
            ResponseAndHostingLog.InvalidPromptMode(Logger, request.PromptModes.ToSpaceSeparatedString());
            throw new ArgumentException("Invalid PromptMode");
        }

        var consentRequired = await Consent.RequiresConsentAsync(request.Subject, request.Client, request.ValidatedResources.ParsedScopes);

        if (consentRequired && request.PromptModes.Contains(OidcConstants.PromptModes.None))
        {
            ResponseAndHostingLog.ConsentRequiredWithPromptNone(Logger);

            return new InteractionResponse
            {
                Error = OidcConstants.AuthorizeErrors.ConsentRequired
            };
        }

        if (request.PromptModes.Contains(OidcConstants.PromptModes.Consent) || consentRequired)
        {
            var response = new InteractionResponse();

            if (consent == null)
            {
                response.IsConsent = true;
                ResponseAndHostingLog.ShowingConsent(Logger);
            }
            else
            {
                request.WasConsentShown = true;
                ResponseAndHostingLog.ConsentWasShown(Logger);

                if (consent.Granted == false)
                {
                    ResponseAndHostingLog.UserConsentResultError(Logger, consent.Error);

                    var error = consent.Error switch
                    {
                        AuthorizationError.AccountSelectionRequired => OidcConstants.AuthorizeErrors.AccountSelectionRequired,
                        AuthorizationError.ConsentRequired => OidcConstants.AuthorizeErrors.ConsentRequired,
                        AuthorizationError.InteractionRequired => OidcConstants.AuthorizeErrors.InteractionRequired,
                        AuthorizationError.LoginRequired => OidcConstants.AuthorizeErrors.LoginRequired,
                        _ => OidcConstants.AuthorizeErrors.AccessDenied
                    };

                    response.Error = error;
                    response.ErrorDescription = consent.ErrorDescription;
                }
                else
                {
                    var requiredScopes = request.ValidatedResources.GetRequiredScopeValues();
                    var consented = consent.ScopesValuesConsented ?? [];
                    var valid = requiredScopes.All(x => consented.Contains(x));
                    if (valid == false)
                    {
                        response.Error = OidcConstants.AuthorizeErrors.AccessDenied;
                        ResponseAndHostingLog.UserDeniedRequiredScopes(Logger);
                    }
                    else
                    {
                        request.Description = consent.Description;
                        request.ValidatedResources = request.ValidatedResources.Filter(consented);
                        ResponseAndHostingLog.UserConsentedToScopes(Logger, consented);

                        if (request.Client.AllowRememberConsent)
                        {
                            var parsedScopes = Enumerable.Empty<ParsedScopeValue>();
                            if (consent.RememberConsent)
                            {
                                parsedScopes = request.ValidatedResources.ParsedScopes;
                                ResponseAndHostingLog.UserRememberedConsent(Logger, request.ValidatedResources.RawScopeValues);
                            }

                            await Consent.UpdateConsentAsync(request.Subject, request.Client, parsedScopes);
                        }
                    }
                }
            }

            return response;
        }

        return new InteractionResponse();
    }
}
