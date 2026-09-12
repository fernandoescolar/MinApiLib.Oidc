namespace MinApiLib.OidcServer.Core.Extensions;

internal static class ModelEnumerableExtensions
{
    [DebuggerStepThrough]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsNullOrEmpty<T>([System.Diagnostics.CodeAnalysis.NotNullWhen(false)] this IEnumerable<T>? list)
        => list is null || !list.Any();
}
