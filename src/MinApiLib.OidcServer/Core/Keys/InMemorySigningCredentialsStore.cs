namespace MinApiLib.OidcServer.Core.Keys;

/// <summary>
/// Default signing credentials store
/// </summary>
/// <seealso cref="MinApiLib.OidcServer.Core.Keys.ISigningCredentialStore" />
public class InMemorySigningCredentialsStore : ISigningCredentialStore
{
    private readonly SigningCredentials _credential;

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemorySigningCredentialsStore"/> class.
    /// </summary>
    /// <param name="credential">The credential.</param>
    public InMemorySigningCredentialsStore(SigningCredentials credential)
    {
        _credential = credential;
    }

    /// <summary>
    /// Gets the signing credentials.
    /// </summary>
    /// <returns></returns>
    public Task<SigningCredentials> GetSigningCredentialsAsync()
    {
        return Task.FromResult(_credential);
    }
}
