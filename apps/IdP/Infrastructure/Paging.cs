namespace IdP.Infrastructure;

/// <summary>A page of results of a list API.</summary>
public sealed record Page<T>(IReadOnlyList<T> Items, long Total, int PageNumber, int PageSize);

public static class Paging
{
    public static int Size(int? pageSize) => Math.Clamp(pageSize ?? 25, 1, 200);

    public static int Number(int? page) => Math.Max(page ?? 1, 1);

    public static int Skip(int? page, int? pageSize) => (Number(page) - 1) * Size(pageSize);
}

/// <summary>Validation errors of an API body, as RFC 9457 problem details.</summary>
public static class ApiErrors
{
    public static IResult Validation(string message, string? field = null)
        => Results.ValidationProblem(new Dictionary<string, string[]> { [field ?? string.Empty] = [message] });

    public static IResult Identity(IdentityResult result)
        => Results.ValidationProblem(result.Errors.GroupBy(e => e.Code).ToDictionary(g => g.Key, g => g.Select(e => e.Description).ToArray()));
}
