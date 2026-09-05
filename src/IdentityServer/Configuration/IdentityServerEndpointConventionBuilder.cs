namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Conventions for the endpoints mapped by <c>MapIdentityServer()</c>. Conventions added here apply to every
/// endpoint; use <see cref="ForEndpoint"/> to target one of them.
/// </summary>
public sealed class IdentityServerEndpointConventionBuilder : IEndpointConventionBuilder
{
    private readonly IReadOnlyList<(string Name, IEndpointConventionBuilder Builder)> _endpoints;

    internal IdentityServerEndpointConventionBuilder(IReadOnlyList<(string Name, IEndpointConventionBuilder Builder)> endpoints)
    {
        _endpoints = endpoints;
    }

    /// <inheritdoc />
    public void Add(Action<EndpointBuilder> convention)
    {
        foreach (var endpoint in _endpoints)
        {
            endpoint.Builder.Add(convention);
        }
    }

    /// <inheritdoc />
    public void Finally(Action<EndpointBuilder> finallyConvention)
    {
        foreach (var endpoint in _endpoints)
        {
            endpoint.Builder.Finally(finallyConvention);
        }
    }

    /// <summary>
    /// Returns a builder for the endpoints registered under <paramref name="name"/>, including their mTLS aliases.
    /// </summary>
    /// <param name="name">The endpoint name, see <see cref="IdentityServerConstants.EndpointNames"/>.</param>
    public IEndpointConventionBuilder ForEndpoint(string name)
    {
        var builders = _endpoints.Where(e => string.Equals(e.Name, name, StringComparison.Ordinal)).Select(e => e.Builder).ToList();
        if (builders.Count == 0)
        {
            throw new InvalidOperationException($"No IdentityServer endpoint named '{name}' is mapped.");
        }

        return new CompositeConventionBuilder(builders);
    }

    private sealed class CompositeConventionBuilder : IEndpointConventionBuilder
    {
        private readonly IReadOnlyList<IEndpointConventionBuilder> _builders;

        public CompositeConventionBuilder(IReadOnlyList<IEndpointConventionBuilder> builders)
        {
            _builders = builders;
        }

        public void Add(Action<EndpointBuilder> convention)
        {
            foreach (var builder in _builders)
            {
                builder.Add(convention);
            }
        }

        public void Finally(Action<EndpointBuilder> finallyConvention)
        {
            foreach (var builder in _builders)
            {
                builder.Finally(finallyConvention);
            }
        }
    }
}
