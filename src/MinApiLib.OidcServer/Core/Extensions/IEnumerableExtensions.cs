namespace MinApiLib.OidcServer.Core.Extensions;

public static class IEnumerableExtensions
{
    public static bool HasDuplicates<T, TProp>(this IEnumerable<T> list, Func<T, TProp> selector)
    {
        var d = new HashSet<TProp>();
        foreach (var t in list)
        {
            if (!d.Add(selector(t)))
            {
                return true;
            }
        }
        return false;
    }
}
