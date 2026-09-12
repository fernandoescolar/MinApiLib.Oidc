namespace MinApiLib.OidcServer.Core.ResourceValidation;

internal static class ScopeExtensions
{
    [DebuggerStepThrough]
    public static string ToSpaceSeparatedString(this IEnumerable<ApiScope> apiScopes)
    {
        var scopeNames = from s in apiScopes
                         select s.Name;

        return string.Join(" ", scopeNames.ToArray());
    }

    [DebuggerStepThrough]
    public static IEnumerable<string> ToStringList(this IEnumerable<ApiScope> apiScopes)
    {
        var scopeNames = from s in apiScopes
                         select s.Name;

        return scopeNames;
    }
}
