namespace IdentityServer.ResponseHandling;

/// <summary>
/// The userinfo response generator
/// </summary>
/// <seealso cref="IdentityServer.ResponseHandling.IUserInfoResponseGenerator" />
public class UserInfoResponseGenerator : IUserInfoResponseGenerator
{
    /// <summary>
    /// The logger
    /// </summary>
    protected readonly ILogger Logger;

    /// <summary>
    /// The profile service
    /// </summary>
    protected readonly IProfileService Profile;

    /// <summary>
    /// The resource store
    /// </summary>
    protected readonly IResourceStore Resources;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserInfoResponseGenerator"/> class.
    /// </summary>
    /// <param name="profile">The profile.</param>
    /// <param name="resourceStore">The resource store.</param>
    /// <param name="logger">The logger.</param>
    public UserInfoResponseGenerator(IProfileService profile, IResourceStore resourceStore, ILogger<UserInfoResponseGenerator> logger)
    {
        Profile = profile;
        Resources = resourceStore;
        Logger = logger;
    }

    /// <summary>
    /// Creates the response.
    /// </summary>
    /// <param name="validationResult">The userinfo request validation result.</param>
    /// <returns></returns>
    /// <exception cref="System.InvalidOperationException">Profile service returned incorrect subject value</exception>
    public virtual async Task<Dictionary<string, object>> ProcessAsync(UserInfoRequestValidationResult validationResult)
    {
        ResponseAndHostingLog.CreatingUserInfoResponse(Logger);

        var scopes = validationResult.TokenValidationResult.Claims.Where(c => c.Type == JwtClaimTypes.Scope).Select(c => c.Value);

        var validatedResources = await GetRequestedResourcesAsync(scopes);
        var requestedClaimTypes = await GetRequestedClaimTypesAsync(validatedResources);

        ResponseAndHostingLog.RequestedClaimTypes(Logger, requestedClaimTypes.ToSpaceSeparatedString());

        var context = new ProfileDataRequestContext(
            validationResult.Subject,
            validationResult.TokenValidationResult.Client ?? throw new InvalidOperationException("The access token has no client."),
            IdentityServerConstants.ProfileDataCallers.UserInfoEndpoint,
            requestedClaimTypes);
        context.RequestedResources = validatedResources;

        await Profile.GetProfileDataAsync(context);
        var profileClaims = context.IssuedClaims;

        var outgoingClaims = new List<Claim>();

        if (profileClaims == null)
        {
            ResponseAndHostingLog.ProfileServiceReturnedNoClaims(Logger);
        }
        else
        {
            outgoingClaims.AddRange(profileClaims);
            ResponseAndHostingLog.ProfileServiceReturnedClaimTypes(Logger, profileClaims.Select(c => c.Type).ToSpaceSeparatedString());
        }

        var subClaim = outgoingClaims.SingleOrDefault(x => x.Type == JwtClaimTypes.Subject);
        if (subClaim == null)
        {
            outgoingClaims.Add(new Claim(JwtClaimTypes.Subject, validationResult.Subject.GetSubjectId()));
        }
        else if (subClaim.Value != validationResult.Subject.GetSubjectId())
        {
            ResponseAndHostingLog.ProfileServiceReturnedIncorrectSubject(Logger, subClaim);
            throw new InvalidOperationException("Profile service returned incorrect subject value");
        }

        return outgoingClaims.ToClaimsDictionary();
    }

    /// <summary>
    ///  Gets the identity resources from the scopes.
    /// </summary>
    /// <param name="scopes"></param>
    /// <returns></returns>
    protected internal virtual async Task<ResourceValidationResult?> GetRequestedResourcesAsync(IEnumerable<string> scopes)
    {
        if (scopes == null || !scopes.Any())
        {
            return null;
        }

        var scopeString = string.Join(" ", scopes);
        ResponseAndHostingLog.ScopesInAccessToken(Logger, scopeString);

        var identityResources = await Resources.FindEnabledIdentityResourcesByScopeAsync(scopes);

        var resources = new Resources(identityResources, Enumerable.Empty<ApiResource>(), Enumerable.Empty<ApiScope>());
        var result = new ResourceValidationResult(resources);

        return result;
    }

    /// <summary>
    /// Gets the requested claim types.
    /// </summary>
    /// <param name="resourceValidationResult"></param>
    /// <returns></returns>
    protected internal virtual Task<IEnumerable<string>> GetRequestedClaimTypesAsync(ResourceValidationResult? resourceValidationResult)
    {
        IEnumerable<string> result;

        if (resourceValidationResult == null)
        {
            result = Enumerable.Empty<string>();
        }
        else
        {
            var identityResources = resourceValidationResult.Resources.IdentityResources;
            result = identityResources.SelectMany(x => x.UserClaims).Distinct();
        }

        return Task.FromResult(result);
    }
}
