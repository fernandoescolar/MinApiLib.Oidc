namespace IdentityServer.Extensions;

internal static class ModelStringExtensions
{
    [DebuggerStepThrough]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsMissing([System.Diagnostics.CodeAnalysis.NotNullWhen(false)] this string? value)
        => string.IsNullOrWhiteSpace(value);

    [DebuggerStepThrough]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsPresent([System.Diagnostics.CodeAnalysis.NotNullWhen(true)] this string? value)
        => !string.IsNullOrWhiteSpace(value);
}
