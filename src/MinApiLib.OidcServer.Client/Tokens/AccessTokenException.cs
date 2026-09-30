namespace MinApiLib.OidcServer.Client.Tokens;

/// <summary>
/// An access token could not be obtained: the token endpoint answered an error, or there is no signed-in user with
/// an access token.
/// </summary>
public sealed class AccessTokenException : Exception
{
    /// <summary>Creates the exception.</summary>
    public AccessTokenException()
    {
    }

    /// <summary>Creates the exception.</summary>
    public AccessTokenException(string message) : base(message)
    {
    }

    /// <summary>Creates the exception.</summary>
    public AccessTokenException(string message, Exception innerException) : base(message, innerException)
    {
    }

    /// <summary>Creates the exception for an error of the token endpoint.</summary>
    /// <param name="error">The OAuth error code, such as <c>invalid_grant</c>.</param>
    /// <param name="errorDescription">The error description, if the server sent one.</param>
    public AccessTokenException(string error, string? errorDescription)
        : base(errorDescription == null ? $"The token endpoint answered '{error}'." : $"The token endpoint answered '{error}': {errorDescription}")
    {
        Error = error;
        ErrorDescription = errorDescription;
    }

    /// <summary>Gets the OAuth error code of the token endpoint, such as <c>invalid_grant</c>.</summary>
    public string? Error { get; }

    /// <summary>Gets the error description of the token endpoint.</summary>
    public string? ErrorDescription { get; }
}
