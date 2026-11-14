// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// Derived from IdentityServer4 4.1.2 (https://github.com/IdentityServer/IdentityServer4); modified for MinApiLib.Oidc.

namespace MinApiLib.Oidc.Server.Core.TokenServices;

/// <summary>
/// Default claims provider implementation
/// </summary>
[RegisterInIServiceCollection(typeof(IClaimsService))]
public class DefaultClaimsService : IClaimsService
{
    /// <summary>
    /// The logger
    /// </summary>
    protected readonly ILogger Logger;

    /// <summary>
    /// The user service
    /// </summary>
    protected readonly IProfileService Profile;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultClaimsService"/> class.
    /// </summary>
    /// <param name="profile">The profile service</param>
    /// <param name="logger">The logger</param>
    public DefaultClaimsService(IProfileService profile, ILogger<DefaultClaimsService> logger)
    {
        Logger = logger;
        Profile = profile;
    }

    /// <summary>
    /// Returns claims for an identity token
    /// </summary>
    /// <param name="subject">The subject</param>
    /// <param name="resources">The requested resources</param>
    /// <param name="includeAllIdentityClaims">Specifies if all claims should be included in the token, or if the userinfo endpoint can be used to retrieve them</param>
    /// <param name="request">The raw request</param>
    /// <returns>
    /// Claims for the identity token
    /// </returns>
    public virtual async Task<IEnumerable<Claim>> GetIdentityTokenClaimsAsync(ClaimsPrincipal subject, ResourceValidationResult resources, bool includeAllIdentityClaims, ValidatedRequest request)
    {
        ServicesLog.GettingIdentityTokenClaims(Logger,
            subject.GetSubjectId(),
            request.Client.ClientId);

        var outputClaims = new List<Claim>(GetStandardSubjectClaims(subject));
        outputClaims.AddRange(GetOptionalClaims(subject));

        if (includeAllIdentityClaims || request.Client.AlwaysIncludeUserClaimsInIdToken)
        {
            var additionalClaimTypes = new List<string>();

            foreach (var identityResource in resources.Resources.IdentityResources)
            {
                foreach (var userClaim in identityResource.UserClaims)
                {
                    additionalClaimTypes.Add(userClaim);
                }
            }

            additionalClaimTypes = FilterRequestedClaimTypes(additionalClaimTypes).ToList();

            var context = new ProfileDataRequestContext(
                subject,
                request.Client,
                OidcServerConstants.ProfileDataCallers.ClaimsProviderIdentityToken,
                additionalClaimTypes)
            {
                RequestedResources = resources,
                ValidatedRequest = request
            };

            await Profile.GetProfileDataAsync(context);

            var claims = FilterProtocolClaims(context.IssuedClaims);
            if (claims != null)
            {
                outputClaims.AddRange(claims);
            }
        }
        else
        {
            ServicesLog.IdTokenUserClaimsOmitted(Logger);
        }

        return outputClaims;
    }

    /// <summary>
    /// Returns claims for an access token.
    /// </summary>
    /// <param name="subject">The subject.</param>
    /// <param name="resources">The validated resource result</param>
    /// <param name="request">The raw request.</param>
    /// <returns>
    /// Claims for the access token
    /// </returns>
    public virtual async Task<IEnumerable<Claim>> GetAccessTokenClaimsAsync(ClaimsPrincipal? subject, ResourceValidationResult resources, ValidatedRequest request)
    {
        ServicesLog.GettingAccessTokenClaimsForClient(Logger, request.Client.ClientId);

        var outputClaims = new List<Claim>
        {
            new Claim(JwtClaimTypes.ClientId, request.ClientId)
        };

        if (!string.Equals(request.ClientId, request.Client.ClientId, StringComparison.Ordinal))
        {
            ServicesLog.ClientImpersonating(Logger, request.Client.ClientId, request.ClientId);
        }

        if (request.ClientClaims != null && request.ClientClaims.Count > 0)
        {
            if (subject == null || request.Client.AlwaysSendClientClaims)
            {
                foreach (var claim in request.ClientClaims)
                {
                    var claimType = claim.Type;

                    if (request.Client.ClientClaimsPrefix.IsPresent())
                    {
                        claimType = request.Client.ClientClaimsPrefix + claimType;
                    }

                    outputClaims.Add(new Claim(claimType, claim.Value, claim.ValueType));
                }
            }
        }

        foreach (var scope in resources.RawScopeValues.Where(x => x != OidcServerConstants.StandardScopes.OfflineAccess))
        {
            outputClaims.Add(new Claim(JwtClaimTypes.Scope, scope));
        }

        if (subject != null)
        {
            if (resources.Resources.OfflineAccess)
            {
                outputClaims.Add(new Claim(JwtClaimTypes.Scope, OidcServerConstants.StandardScopes.OfflineAccess));
            }

            ServicesLog.GettingAccessTokenClaimsForSubject(Logger, subject.GetSubjectId());

            outputClaims.AddRange(GetStandardSubjectClaims(subject));
            outputClaims.AddRange(GetOptionalClaims(subject));

            var additionalClaimTypes = new List<string>();
            foreach (var api in resources.Resources.ApiResources)
            {
                if (api.UserClaims != null)
                {
                    foreach (var claim in api.UserClaims)
                    {
                        additionalClaimTypes.Add(claim);
                    }
                }
            }

            foreach (var scope in resources.Resources.ApiScopes)
            {
                if (scope.UserClaims != null)
                {
                    foreach (var claim in scope.UserClaims)
                    {
                        additionalClaimTypes.Add(claim);
                    }
                }
            }

            additionalClaimTypes = FilterRequestedClaimTypes(additionalClaimTypes).ToList();

            var context = new ProfileDataRequestContext(
                subject,
                request.Client,
                OidcServerConstants.ProfileDataCallers.ClaimsProviderAccessToken,
                additionalClaimTypes.Distinct())
            {
                RequestedResources = resources,
                ValidatedRequest = request
            };

            await Profile.GetProfileDataAsync(context);

            var claims = FilterProtocolClaims(context.IssuedClaims);
            if (claims != null)
            {
                outputClaims.AddRange(claims);
            }
        }

        return outputClaims;
    }

    /// <summary>
    /// Gets the standard subject claims.
    /// </summary>
    /// <param name="subject">The subject.</param>
    /// <returns>A list of standard claims</returns>
    protected virtual IEnumerable<Claim> GetStandardSubjectClaims(ClaimsPrincipal subject)
    {
        var claims = new List<Claim>
        {
            new Claim(JwtClaimTypes.Subject, subject.GetSubjectId()),
            new Claim(JwtClaimTypes.AuthenticationTime, subject.GetAuthenticationTimeEpoch().ToString(CultureInfo.InvariantCulture), ClaimValueTypes.Integer64),
            new Claim(JwtClaimTypes.IdentityProvider, subject.GetIdentityProvider())
        };

        claims.AddRange(subject.GetAuthenticationMethods());

        return claims;
    }

    /// <summary>
    /// Gets additional (and optional) claims from the cookie or incoming subject.
    /// </summary>
    /// <param name="subject">The subject.</param>
    /// <returns>Additional claims</returns>
    protected virtual IEnumerable<Claim> GetOptionalClaims(ClaimsPrincipal subject)
    {
        var claims = new List<Claim>();

        var acr = subject.FindFirst(JwtClaimTypes.AuthenticationContextClassReference);
        if (acr != null) claims.Add(acr);

        return claims;
    }

    /// <summary>
    /// Filters out protocol claims like amr, nonce etc..
    /// </summary>
    /// <param name="claims">The claims.</param>
    /// <returns></returns>
    protected virtual IEnumerable<Claim> FilterProtocolClaims(IEnumerable<Claim> claims)
    {
        var claimsToFilter = claims.Where(x => Constants.Filters.ClaimsServiceFilterClaimTypes.Contains(x.Type));
        if (claimsToFilter.Any())
        {
            var types = claimsToFilter.Select(x => x.Type);
            ServicesLog.ProfileClaimTypesFiltered(Logger, types);
        }
        return claims.Except(claimsToFilter);
    }

    /// <summary>
    /// Filters out protocol claims like amr, nonce etc..
    /// </summary>
    /// <param name="claimTypes">The claim types.</param>
    protected virtual IEnumerable<string> FilterRequestedClaimTypes(IEnumerable<string> claimTypes)
    {
        var claimTypesToFilter = claimTypes.Where(x => Constants.Filters.ClaimsServiceFilterClaimTypes.Contains(x));
        return claimTypes.Except(claimTypesToFilter);
    }
}
