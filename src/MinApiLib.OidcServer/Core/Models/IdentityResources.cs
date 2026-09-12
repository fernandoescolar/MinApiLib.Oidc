namespace MinApiLib.OidcServer.Core.Models;

/// <summary>
/// Convenience class that defines standard identity resources.
/// </summary>
public static class IdentityResources
{
    /// <summary>
    /// Models the standard openid scope
    /// </summary>
    /// <seealso cref="MinApiLib.OidcServer.Core.Models.IdentityResource" />
    public class OpenId : IdentityResource
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OpenId"/> class.
        /// </summary>
        public OpenId()
        {
            Name = OidcServerConstants.StandardScopes.OpenId;
            DisplayName = "Your user identifier";
            Required = true;
            UserClaims.Add(JwtClaimTypes.Subject);
        }
    }

    /// <summary>
    /// Models the standard profile scope
    /// </summary>
    /// <seealso cref="MinApiLib.OidcServer.Core.Models.IdentityResource" />
    public class Profile : IdentityResource
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Profile"/> class.
        /// </summary>
        public Profile()
        {
            Name = OidcServerConstants.StandardScopes.Profile;
            DisplayName = "User profile";
            Description = "Your user profile information (first name, last name, etc.)";
            Emphasize = true;
            UserClaims = Constants.ScopeToClaimsMapping[OidcServerConstants.StandardScopes.Profile].ToList();
        }
    }

    /// <summary>
    /// Models the standard email scope
    /// </summary>
    /// <seealso cref="MinApiLib.OidcServer.Core.Models.IdentityResource" />
    public class Email : IdentityResource
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Email"/> class.
        /// </summary>
        public Email()
        {
            Name = OidcServerConstants.StandardScopes.Email;
            DisplayName = "Your email address";
            Emphasize = true;
            UserClaims = (Constants.ScopeToClaimsMapping[OidcServerConstants.StandardScopes.Email].ToList());
        }
    }

    /// <summary>
    /// Models the standard phone scope
    /// </summary>
    /// <seealso cref="MinApiLib.OidcServer.Core.Models.IdentityResource" />
    public class Phone : IdentityResource
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Phone"/> class.
        /// </summary>
        public Phone()
        {
            Name = OidcServerConstants.StandardScopes.Phone;
            DisplayName = "Your phone number";
            Emphasize = true;
            UserClaims = Constants.ScopeToClaimsMapping[OidcServerConstants.StandardScopes.Phone].ToList();
        }
    }

    /// <summary>
    /// Models the standard address scope
    /// </summary>
    /// <seealso cref="MinApiLib.OidcServer.Core.Models.IdentityResource" />
    public class Address : IdentityResource
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Address"/> class.
        /// </summary>
        public Address()
        {
            Name = OidcServerConstants.StandardScopes.Address;
            DisplayName = "Your postal address";
            Emphasize = true;
            UserClaims = Constants.ScopeToClaimsMapping[OidcServerConstants.StandardScopes.Address].ToList();
        }
    }
}
