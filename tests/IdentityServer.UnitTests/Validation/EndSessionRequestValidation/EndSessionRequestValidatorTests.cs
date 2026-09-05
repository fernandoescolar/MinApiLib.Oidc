namespace IdentityServer.UnitTests.Validation.EndSessionRequestValidation;

public class EndSessionRequestValidatorTests
{
    private EndSessionRequestValidator _subject;
    private IdentityServerOptions _options;
    private StubTokenValidator _stubTokenValidator = new StubTokenValidator();
    private StubRedirectUriValidator _stubRedirectUriValidator = new StubRedirectUriValidator();
    private MockHttpContextAccessor _context = new MockHttpContextAccessor();
    private MockUserSession _userSession = new MockUserSession();
    private MockLogoutNotificationService _mockLogoutNotificationService = new MockLogoutNotificationService();
    private MockMessageStore<LogoutNotificationContext> _mockEndSessionMessageStore = new MockMessageStore<LogoutNotificationContext>();

    private readonly List<Client> _clients = new()
    {
        new Client { ClientId = "client", PostLogoutRedirectUris = { "http://client/signout-cb" } },
        new Client { ClientId = "disabled", Enabled = false },
        new Client { ClientId = "wsfed", ProtocolType = "wsfed" }
    };

    private ClaimsPrincipal _user;

    public EndSessionRequestValidatorTests()
    {
        _user = new IdentityServerUser("alice").CreatePrincipal();

        _options = TestIdentityServerOptions.Create();
        _subject = new EndSessionRequestValidator(
            _context,
            _options,
            _stubTokenValidator,
            _stubRedirectUriValidator,
            _userSession,
            _mockLogoutNotificationService,
            _mockEndSessionMessageStore,
            new InMemoryClientStore(_clients),
            TestLogger.Create<EndSessionRequestValidator>());
    }

    [Fact]
    public async Task anonymous_user_when_options_require_authenticated_user_should_return_error()
    {
        _options.Authentication.RequireAuthenticatedUserForSignOutMessage = true;

        var parameters = new NameValueCollection();
        var result = await _subject.ValidateAsync(parameters, null);
        Assert.True(result.IsError);

        result = await _subject.ValidateAsync(parameters, new ClaimsPrincipal());
        Assert.True(result.IsError);

        result = await _subject.ValidateAsync(parameters, new ClaimsPrincipal(new ClaimsIdentity()));
        Assert.True(result.IsError);
    }

    [Fact]
    public async Task valid_params_should_return_success()
    {
        _stubTokenValidator.IdentityTokenValidationResult = new TokenValidationResult()
        {
            IsError = false,
            Claims = new Claim[] { new Claim("sub", _user.GetSubjectId()) },
            Client = new Client() { ClientId = "client" }
        };
        _stubRedirectUriValidator.IsPostLogoutRedirectUriValid = true;

        var parameters = new NameValueCollection();
        parameters.Add("id_token_hint", "id_token");
        parameters.Add("post_logout_redirect_uri", "http://client/signout-cb");
        parameters.Add("client_id", "client");
        parameters.Add("state", "foo");

        var result = await _subject.ValidateAsync(parameters, _user);
        Assert.False(result.IsError);

        Assert.Equal("client", result.ValidatedRequest.Client.ClientId);
        Assert.Equal("http://client/signout-cb", result.ValidatedRequest.PostLogOutUri);
        Assert.Equal("foo", result.ValidatedRequest.State);
        Assert.Equal(_user.GetSubjectId(), result.ValidatedRequest.Subject.GetSubjectId());
    }

    [Fact]
    public async Task no_post_logout_redirect_uri_should_not_use_single_registered_uri()
    {
        _stubTokenValidator.IdentityTokenValidationResult = new TokenValidationResult()
        {
            IsError = false,
            Claims = new Claim[] { new Claim("sub", _user.GetSubjectId()) },
            Client = new Client() { ClientId = "client1", PostLogoutRedirectUris = new List<string> { "foo" } }
        };
        _stubRedirectUriValidator.IsPostLogoutRedirectUriValid = true;

        var parameters = new NameValueCollection();
        parameters.Add("id_token_hint", "id_token");

        var result = await _subject.ValidateAsync(parameters, _user);
        Assert.False(result.IsError);
        Assert.Null(result.ValidatedRequest.PostLogOutUri);
    }

    [Fact]
    public async Task no_post_logout_redirect_uri_should_not_use_multiple_registered_uri()
    {
        _stubTokenValidator.IdentityTokenValidationResult = new TokenValidationResult()
        {
            IsError = false,
            Claims = new Claim[] { new Claim("sub", _user.GetSubjectId()) },
            Client = new Client() { ClientId = "client1", PostLogoutRedirectUris = new List<string> { "foo", "bar" } }
        };
        _stubRedirectUriValidator.IsPostLogoutRedirectUriValid = true;

        var parameters = new NameValueCollection();
        parameters.Add("id_token_hint", "id_token");

        var result = await _subject.ValidateAsync(parameters, _user);
        Assert.False(result.IsError);
        Assert.Null(result.ValidatedRequest.PostLogOutUri);
    }

    [Fact]
    public async Task post_logout_uri_fails_validation_should_not_honor_logout_uri()
    {
        _stubTokenValidator.IdentityTokenValidationResult = new TokenValidationResult()
        {
            IsError = false,
            Claims = new Claim[] { new Claim("sub", _user.GetSubjectId()) },
            Client = new Client() { ClientId = "client" }
        };
        _stubRedirectUriValidator.IsPostLogoutRedirectUriValid = false;

        var parameters = new NameValueCollection();
        parameters.Add("id_token_hint", "id_token");
        parameters.Add("post_logout_redirect_uri", "http://client/signout-cb");
        parameters.Add("client_id", "client");
        parameters.Add("state", "foo");

        var result = await _subject.ValidateAsync(parameters, _user);
        Assert.False(result.IsError);

        Assert.Equal("client", result.ValidatedRequest.Client.ClientId);
        Assert.Equal(_user.GetSubjectId(), result.ValidatedRequest.Subject.GetSubjectId());

        Assert.Null(result.ValidatedRequest.State);
        Assert.Null(result.ValidatedRequest.PostLogOutUri);
    }

    [Fact]
    public async Task subject_mismatch_should_return_error()
    {
        _stubTokenValidator.IdentityTokenValidationResult = new TokenValidationResult()
        {
            IsError = false,
            Claims = new Claim[] { new Claim("sub", "xoxo") },
            Client = new Client() { ClientId = "client" }
        };
        _stubRedirectUriValidator.IsPostLogoutRedirectUriValid = true;

        var parameters = new NameValueCollection();
        parameters.Add("id_token_hint", "id_token");
        parameters.Add("post_logout_redirect_uri", "http://client/signout-cb");
        parameters.Add("client_id", "client");
        parameters.Add("state", "foo");

        var result = await _subject.ValidateAsync(parameters, _user);
        Assert.True(result.IsError);
    }

    [Fact]
    public async Task successful_request_should_return_inputs()
    {
        var parameters = new NameValueCollection();

        var result = await _subject.ValidateAsync(parameters, _user);
        Assert.False(result.IsError);
        Assert.Same(parameters, result.ValidatedRequest.Raw);
    }

    [Fact]
    public async Task client_id_not_matching_id_token_hint_should_return_error()
    {
        _stubTokenValidator.IdentityTokenValidationResult = new TokenValidationResult()
        {
            IsError = false,
            Claims = new Claim[] { new Claim("sub", _user.GetSubjectId()) },
            Client = new Client() { ClientId = "client" }
        };
        _stubRedirectUriValidator.IsPostLogoutRedirectUriValid = true;

        var parameters = new NameValueCollection();
        parameters.Add("id_token_hint", "id_token");
        parameters.Add("post_logout_redirect_uri", "http://client/signout-cb");
        parameters.Add("client_id", "other_client");

        var result = await _subject.ValidateAsync(parameters, _user);

        Assert.True(result.IsError);
    }

    [Fact]
    public async Task client_id_without_id_token_hint_should_identify_client_and_honor_registered_post_logout_uri()
    {
        _stubRedirectUriValidator.IsPostLogoutRedirectUriValid = true;

        var parameters = new NameValueCollection
        {
            { "client_id", "client" },
            { "post_logout_redirect_uri", "http://client/signout-cb" },
            { "state", "foo" }
        };

        var result = await _subject.ValidateAsync(parameters, _user);

        Assert.False(result.IsError);
        Assert.Equal("client", result.ValidatedRequest.Client.ClientId);
        Assert.Equal("http://client/signout-cb", result.ValidatedRequest.PostLogOutUri);
        Assert.Equal("foo", result.ValidatedRequest.State);
        Assert.Equal(_user.GetSubjectId(), result.ValidatedRequest.Subject.GetSubjectId());
        Assert.False(result.ValidatedRequest.IsAuthenticated);
    }

    [Fact]
    public async Task client_id_without_id_token_hint_should_still_require_the_signout_prompt()
    {
        _stubRedirectUriValidator.IsPostLogoutRedirectUriValid = true;

        var parameters = new NameValueCollection { { "client_id", "client" } };
        var result = await _subject.ValidateAsync(parameters, _user);

        var request = new LogoutRequest(null, new LogoutMessage(result.ValidatedRequest));
        Assert.Equal("client", request.ClientId);
        Assert.True(request.ShowSignoutPrompt);
    }

    [Fact]
    public async Task valid_id_token_hint_should_skip_the_signout_prompt()
    {
        _stubTokenValidator.IdentityTokenValidationResult = new TokenValidationResult
        {
            IsError = false,
            Claims = new[] { new Claim("sub", _user.GetSubjectId()) },
            Client = new Client { ClientId = "client" }
        };

        var parameters = new NameValueCollection { { "id_token_hint", "id_token" } };
        var result = await _subject.ValidateAsync(parameters, _user);

        Assert.True(result.ValidatedRequest.IsAuthenticated);
        Assert.False(new LogoutRequest(null, new LogoutMessage(result.ValidatedRequest)).ShowSignoutPrompt);
    }

    [Fact]
    public async Task client_id_without_id_token_hint_and_unregistered_post_logout_uri_should_not_honor_it()
    {
        _stubRedirectUriValidator.IsPostLogoutRedirectUriValid = false;

        var parameters = new NameValueCollection
        {
            { "client_id", "client" },
            { "post_logout_redirect_uri", "http://attacker/" }
        };

        var result = await _subject.ValidateAsync(parameters, _user);

        Assert.False(result.IsError);
        Assert.Null(result.ValidatedRequest.PostLogOutUri);
    }

    [Theory]
    [InlineData("unknown")]
    [InlineData("disabled")]
    [InlineData("wsfed")]
    public async Task client_id_of_unknown_disabled_or_non_oidc_client_should_return_error(string clientId)
    {
        var parameters = new NameValueCollection { { "client_id", clientId } };

        var result = await _subject.ValidateAsync(parameters, _user);

        Assert.True(result.IsError);
    }

    [Fact]
    public async Task too_long_client_id_should_return_error()
    {
        var parameters = new NameValueCollection { { "client_id", new string('x', _options.InputLengthRestrictions.ClientId + 1) } };

        var result = await _subject.ValidateAsync(parameters, _user);

        Assert.True(result.IsError);
    }
}
