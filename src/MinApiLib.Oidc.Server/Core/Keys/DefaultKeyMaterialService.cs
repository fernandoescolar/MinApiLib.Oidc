namespace MinApiLib.Oidc.Server.Core.Keys;

/// <summary>
/// The default key material service
/// </summary>
/// <seealso cref="MinApiLib.Oidc.Server.Core.Keys.IKeyMaterialService" />
[RegisterInIServiceCollection(typeof(IKeyMaterialService))]
public class DefaultKeyMaterialService : IKeyMaterialService
{
    private readonly IEnumerable<ISigningCredentialStore> _signingCredentialStores;
    private readonly IEnumerable<IValidationKeysStore> _validationKeysStores;
    private readonly ISigningKeyManager? _keyManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultKeyMaterialService"/> class.
    /// </summary>
    /// <param name="validationKeysStores">The validation keys stores.</param>
    /// <param name="signingCredentialStores">The signing credential store.</param>
    /// <param name="keyManager">The automatic signing key manager, if any.</param>
    public DefaultKeyMaterialService(IEnumerable<IValidationKeysStore> validationKeysStores, IEnumerable<ISigningCredentialStore> signingCredentialStores, ISigningKeyManager? keyManager = null)
    {
        _keyManager = keyManager;
        _signingCredentialStores = signingCredentialStores;
        _validationKeysStores = validationKeysStores;
    }

    /// <inheritdoc/>
    public async Task<SigningCredentials?> GetSigningCredentialsAsync(IEnumerable<string>? allowedAlgorithms = null)
    {
        // Automatically managed keys sign for the algorithms they cover; static credentials cover the rest.
        var automatic = await GetAutomaticSigningCredentialsAsync();
        if (automatic.Count > 0)
        {
            var match = allowedAlgorithms == null || !allowedAlgorithms.Any()
                ? automatic[0]
                : automatic.FirstOrDefault(c => allowedAlgorithms.Contains(c.Algorithm));
            if (match != null)
            {
                return match;
            }
        }

        if (_signingCredentialStores.Any())
        {
            if (allowedAlgorithms == null || !allowedAlgorithms.Any())
            {
                return await _signingCredentialStores.First().GetSigningCredentialsAsync();
            }

            var credential = (await GetAllSigningCredentialsAsync()).FirstOrDefault(c => allowedAlgorithms.Contains(c.Algorithm));
            if (credential is null)
            {
                throw new InvalidOperationException($"No signing credential for algorithms ({allowedAlgorithms.ToSpaceSeparatedString()}) registered.");
            }

            return credential;
        }

        return null;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<SigningCredentials>> GetAllSigningCredentialsAsync()
    {
        var credentials = new List<SigningCredentials>(await GetAutomaticSigningCredentialsAsync());

        foreach (var store in _signingCredentialStores)
        {
            credentials.Add(await store.GetSigningCredentialsAsync());
        }

        return credentials;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<SecurityKeyInfo>> GetValidationKeysAsync()
    {
        var keys = new List<SecurityKeyInfo>();

        foreach (var store in _validationKeysStores)
        {
            keys.AddRange(await store.GetValidationKeysAsync());
        }

        if (_keyManager?.IsEnabled == true)
        {
            keys.AddRange(await _keyManager.GetValidationKeysAsync());
        }

        return keys;
    }

    private async Task<IReadOnlyList<SigningCredentials>> GetAutomaticSigningCredentialsAsync()
        => _keyManager?.IsEnabled == true ? await _keyManager.GetSigningCredentialsAsync() : [];
}
