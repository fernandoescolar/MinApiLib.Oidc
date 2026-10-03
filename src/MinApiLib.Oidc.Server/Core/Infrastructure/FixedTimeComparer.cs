namespace MinApiLib.Oidc.Server.Core.Infrastructure;

/// <summary>
/// Compares secrets in time that does not depend on how many characters match.
/// </summary>
internal static class FixedTimeComparer
{
    /// <summary>
    /// Returns <c>true</c> when both strings are equal. Only the length of the inputs can be observed through timing.
    /// </summary>
    public static bool IsEqual(string? left, string? right)
    {
        if (left is null || right is null)
        {
            return left is null && right is null;
        }

        return CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(left), Encoding.UTF8.GetBytes(right));
    }
}
