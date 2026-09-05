namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Conventions for the endpoints mapped by <c>MapOidcServer()</c>. Conventions added here apply to every
/// endpoint; use <see cref="ForEndpoint"/> to target one of them.
/// </summary>
public sealed class OidcServerEndpointConventionBuilder : IEndpointConventionBuilder
{
    private static readonly FrozenSet<string> EndpointNames = typeof(OidcServerConstants.EndpointNames)
        .GetFields(BindingFlags.Public | BindingFlags.Static)
        .Select(f => (string)f.GetRawConstantValue()!)
        .ToFrozenSet(StringComparer.Ordinal);

    private readonly IEndpointConventionBuilder _group;

    internal OidcServerEndpointConventionBuilder(IEndpointConventionBuilder group)
    {
        _group = group;
    }

    /// <inheritdoc />
    public void Add(Action<EndpointBuilder> convention) => _group.Add(convention);

    /// <inheritdoc />
    public void Finally(Action<EndpointBuilder> finallyConvention) => _group.Finally(finallyConvention);

    /// <summary>
    /// Returns a builder for the endpoints named <paramref name="name"/>, including their mTLS aliases.
    /// </summary>
    /// <param name="name">The endpoint name, see <see cref="OidcServerConstants.EndpointNames"/>.</param>
    public IEndpointConventionBuilder ForEndpoint(string name)
    {
        if (!EndpointNames.Contains(name))
        {
            throw new InvalidOperationException($"No OidcServer endpoint named '{name}' is mapped.");
        }

        return new NamedEndpointConventionBuilder(_group, name);
    }

    /// <summary>
    /// Applies conventions only to the endpoints with that name. The name is endpoint metadata, added by the endpoint
    /// record itself after the group's conventions run, so the conventions run as <c>Finally</c> conventions.
    /// </summary>
    private sealed class NamedEndpointConventionBuilder : IEndpointConventionBuilder
    {
        private readonly IEndpointConventionBuilder _group;
        private readonly string _name;

        public NamedEndpointConventionBuilder(IEndpointConventionBuilder group, string name)
        {
            _group = group;
            _name = name;
        }

        public void Add(Action<EndpointBuilder> convention) => _group.Finally(builder =>
        {
            if (IsNamed(builder))
            {
                convention(builder);
            }
        });

        public void Finally(Action<EndpointBuilder> finallyConvention) => Add(finallyConvention);

        private bool IsNamed(EndpointBuilder builder)
            => builder.Metadata.OfType<OidcServerEndpointMetadata>().Any(m => string.Equals(m.Name, _name, StringComparison.Ordinal));
    }
}
