using System.Text.Json;
using IdentityServer.Stores.Serialization;

namespace IdentityServer.UnitTests.Stores.Serialization;

/// <summary>
/// The source-generated grant serializer must write exactly what the reflection-based one wrote, so grants stored
/// before the change keep deserializing and new ones stay readable by older versions.
/// </summary>
public class PersistentGrantSerializerCompatibilityTests
{
    private static readonly JsonSerializerOptions ReflectionOptions = CreateReflectionOptions();

    private static JsonSerializerOptions CreateReflectionOptions()
    {
        var options = new JsonSerializerOptions
        {
            IgnoreReadOnlyProperties = true,
            TypeInfoResolver = new System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver()
        };
        options.Converters.Add(new ClaimConverter());
        options.Converters.Add(new ClaimsPrincipalConverter());
        return options;
    }

    private static readonly DateTime Created = new(2026, 11, 15, 10, 0, 0, DateTimeKind.Utc);

    private static ClaimsPrincipal User()
        => new IdentityServerUser("bob") { AuthenticationTime = Created, AdditionalClaims = { new Claim("tenant", "contoso") } }.CreatePrincipal();

    private static Token AccessToken() => new(OidcConstants.TokenTypes.AccessToken)
    {
        CreationTime = Created,
        Lifetime = 3600,
        Issuer = "https://server",
        ClientId = "client",
        Audiences = { "api1" },
        Confirmation = "{\"jkt\":\"abc\"}",
        Claims = { new Claim("sub", "bob"), new Claim("scope", "api1") }
    };

    public static TheoryData<object> Grants => new()
    {
        new AuthorizationCode
        {
            CreationTime = Created, Lifetime = 300, ClientId = "client", Subject = User(), IsOpenId = true,
            RequestedScopes = ["openid", "api1"], RequestedResourceIndicators = ["https://api1"], RedirectUri = "https://client/cb",
            Nonce = "n", CodeChallenge = "c", CodeChallengeMethod = "S256", DPoPKeyThumbprint = "abc", Properties = { { "k", "v" } }
        },
        new RefreshToken { CreationTime = Created, Lifetime = 600, AccessToken = AccessToken(), AuthorizedResourceIndicators = ["https://api1"] },
        AccessToken(),
        new Consent { SubjectId = "bob", ClientId = "client", Scopes = ["api1"], CreationTime = Created },
        new DeviceCode { CreationTime = Created, Lifetime = 300, ClientId = "client", RequestedScopes = ["api1"], IsOpenId = false },
        new PushedAuthorizationRequest { CreationTime = Created, Lifetime = 600, ClientId = "client", Parameters = { { "scope", ["openid api1"] } } }
    };

    [Theory]
    [MemberData(nameof(Grants))]
    public void Writes_the_same_json_as_the_reflection_serializer(object grant)
    {
        var expected = JsonSerializer.Serialize(grant, grant.GetType(), ReflectionOptions);

        var actual = (string)typeof(PersistentGrantSerializer).GetMethod(nameof(PersistentGrantSerializer.Serialize))!
            .MakeGenericMethod(grant.GetType()).Invoke(new PersistentGrantSerializer(), [grant])!;

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Reads_a_refresh_token_written_by_the_reflection_serializer()
    {
        var json = JsonSerializer.Serialize(new RefreshToken { CreationTime = Created, Lifetime = 600, AccessToken = AccessToken() }, ReflectionOptions);

        var token = new PersistentGrantSerializer().Deserialize<RefreshToken>(json)!;

        Assert.Equal("client", token.AccessToken.ClientId);
        Assert.Equal("bob", token.SubjectId);
        Assert.Equal("{\"jkt\":\"abc\"}", token.AccessToken.Confirmation);
    }

    [Fact]
    public void Custom_grant_types_still_serialize_through_reflection()
    {
        var serializer = new PersistentGrantSerializer();

        var json = serializer.Serialize(new CustomGrant { Value = "x" });

        Assert.Equal("x", serializer.Deserialize<CustomGrant>(json)!.Value);
    }

    public class CustomGrant
    {
        public string Value { get; set; }
    }
}
