namespace MinApiLib.OidcServer.Configuration;

/// <summary>
/// Configures device flow
/// </summary>
public class DeviceFlowOptions
{
    /// <summary>
    /// Gets or sets the default type of the user code.
    /// </summary>
    /// <value>
    /// The default type of the user code.
    /// </value>
    public string DefaultUserCodeType { get; set; } = OidcServerConstants.UserCodeTypes.Numeric;

    /// <summary>
    /// Gets or sets the polling interval in seconds.
    /// </summary>
    /// <value>
    /// The interval in seconds.
    /// </value>
    public int Interval { get; set; } = 5;

    /// <summary>
    /// Maximum number of failed user code lookups allowed for one user (or one client IP address when nobody is
    /// signed in) within <see cref="UserCodeFailedAttemptsWindow"/>. Further lookups fail until the window passes,
    /// which limits brute forcing of user codes (RFC 8628 section 5.1). Zero disables the limit. Defaults to 5.
    /// </summary>
    public int UserCodeMaxFailedAttempts { get; set; } = 5;

    /// <summary>
    /// The window in which failed user code lookups are counted. Defaults to 5 minutes.
    /// </summary>
    public TimeSpan UserCodeFailedAttemptsWindow { get; set; } = TimeSpan.FromMinutes(5);
}
