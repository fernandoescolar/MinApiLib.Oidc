namespace MinApiLib.OidcServer.Services;

/// <summary>
/// Default consent service
/// </summary>
[RegisterInIServiceCollection(typeof(IConsentService))]
public class DefaultConsentService : IConsentService
{
    /// <summary>
    /// The user consent store
    /// </summary>
    protected readonly IUserConsentStore UserConsentStore;

    /// <summary>
    ///  The clock
    /// </summary>
    protected readonly TimeProvider Clock;

    /// <summary>
    /// The logger
    /// </summary>
    protected readonly ILogger<DefaultConsentService> Logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultConsentService" /> class.
    /// </summary>
    /// <param name="clock">The clock.</param>
    /// <param name="userConsentStore">The user consent store.</param>
    /// <param name="logger">The logger.</param>
    /// <exception cref="System.ArgumentNullException">store</exception>
    public DefaultConsentService(TimeProvider clock, IUserConsentStore userConsentStore, ILogger<DefaultConsentService> logger)
    {
        Clock = clock;
        UserConsentStore = userConsentStore;
        Logger = logger;
    }

    /// <summary>
    /// Checks if consent is required.
    /// </summary>
    /// <param name="subject">The user.</param>
    /// <param name="client">The client.</param>
    /// <param name="parsedScopes">The parsed scopes.</param>
    /// <returns>
    /// Boolean if consent is required.
    /// </returns>
    /// <exception cref="System.ArgumentNullException">
    /// client
    /// or
    /// subject
    /// </exception>
    public virtual async Task<bool> RequiresConsentAsync(ClaimsPrincipal subject, Client client, IEnumerable<ParsedScopeValue> parsedScopes)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(subject);

        if (!client.RequireConsent)
        {
            ServicesLog.ClientDoesNotRequireConsent(Logger);
            return false;
        }

        if (parsedScopes == null || !parsedScopes.Any())
        {
            ServicesLog.NoScopesRequestedNoConsentRequired(Logger);
            return false;
        }

        if (!client.AllowRememberConsent)
        {
            ServicesLog.ClientDoesNotAllowRememberConsent(Logger);
            return true;
        }

        if (parsedScopes.Any(x => x.ParsedName != x.RawValue))
        {
            ServicesLog.ParameterizedScopesConsentRequired(Logger);
            return true;
        }

        var scopes = parsedScopes.Select(x => x.RawValue).ToArray();

        if (scopes.Contains(OidcServerConstants.StandardScopes.OfflineAccess))
        {
            ServicesLog.OfflineAccessConsentRequired(Logger);
            return true;
        }

        var consent = await UserConsentStore.GetUserConsentAsync(subject.GetSubjectId(), client.ClientId);

        if (consent == null)
        {
            ServicesLog.NoPriorConsentFound(Logger);
            return true;
        }

        if (consent.Expiration.HasExpired(Clock.GetUtcNow().UtcDateTime))
        {
            ServicesLog.StoredConsentExpired(Logger);
            await UserConsentStore.RemoveUserConsentAsync(consent.SubjectId, consent.ClientId);
            return true;
        }

        if (consent.Scopes != null)
        {
            var intersect = scopes.Intersect(consent.Scopes);
            var different = scopes.Length != intersect.Count();

            if (different)
            {
                ServicesLog.StoredConsentDifferent(Logger);
            }
            else
            {
                ServicesLog.StoredConsentSame(Logger);
            }

            return different;
        }

        ServicesLog.StoredConsentHasNoScopes(Logger);

        return true;
    }

    /// <summary>
    /// Updates the consent asynchronous.
    /// </summary>
    /// <param name="client">The client.</param>
    /// <param name="subject">The subject.</param>
    /// <param name="parsedScopes">The parsed scopes.</param>
    /// <returns></returns>
    /// <exception cref="System.ArgumentNullException">
    /// client
    /// or
    /// subject
    /// </exception>
    public virtual async Task UpdateConsentAsync(ClaimsPrincipal subject, Client client, IEnumerable<ParsedScopeValue> parsedScopes)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(subject);

        if (client.AllowRememberConsent)
        {
            var subjectId = subject.GetSubjectId();
            var clientId = client.ClientId;

            var scopes = parsedScopes?.Select(x => x.RawValue).ToArray();
            if (scopes != null && scopes.Length > 0)
            {
                ServicesLog.UpdatingConsentStore(Logger, subject.GetSubjectId());

                var consent = new Consent
                {
                    CreationTime = Clock.GetUtcNow().UtcDateTime,
                    SubjectId = subjectId,
                    ClientId = clientId,
                    Scopes = scopes
                };

                if (client.ConsentLifetime.HasValue)
                {
                    consent.Expiration = consent.CreationTime.AddSeconds(client.ConsentLifetime.Value);
                }

                await UserConsentStore.StoreUserConsentAsync(consent);
            }
            else
            {
                ServicesLog.RemovingConsentFromStore(Logger, subject.GetSubjectId());

                await UserConsentStore.RemoveUserConsentAsync(subjectId, clientId);
            }
        }
    }
}
