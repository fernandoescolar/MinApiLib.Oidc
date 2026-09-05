namespace IdentityServer.UnitTests.Services.Default;

public class DefaultPersistedGrantServiceTests
{
    private DefaultPersistedGrantService _subject;
    private InMemoryPersistedGrantStore _store = new InMemoryPersistedGrantStore();
    private IAuthorizationCodeStore _codes;
    private IRefreshTokenStore _refreshTokens;
    private IReferenceTokenStore _referenceTokens;
    private IUserConsentStore _userConsent;

    private ClaimsPrincipal _user = new IdentityServerUser("123").CreatePrincipal();

    public DefaultPersistedGrantServiceTests()
    {
        _subject = new DefaultPersistedGrantService(
            _store,
            new PersistentGrantSerializer(),
            TestLogger.Create<DefaultPersistedGrantService>());
        _codes = new DefaultAuthorizationCodeStore(_store,
            new PersistentGrantSerializer(),
            new DefaultHandleGenerationService(),
            TestLogger.Create<DefaultAuthorizationCodeStore>());
        _refreshTokens = new DefaultRefreshTokenStore(_store,
            new PersistentGrantSerializer(),
            new DefaultHandleGenerationService(),
            TestLogger.Create<DefaultRefreshTokenStore>());
        _referenceTokens = new DefaultReferenceTokenStore(_store,
            new PersistentGrantSerializer(),
            new DefaultHandleGenerationService(),
            TestLogger.Create<DefaultReferenceTokenStore>());
        _userConsent = new DefaultUserConsentStore(_store,
            new PersistentGrantSerializer(),
            new DefaultHandleGenerationService(),
            TestLogger.Create<DefaultUserConsentStore>());
    }

    [Fact]
    public async Task GetAllGrantsAsync_should_return_all_grants()
    {
        await _userConsent.StoreUserConsentAsync(new Consent()
        {
            CreationTime = DateTime.UtcNow,
            ClientId = "client1",
            SubjectId = "123",
            Scopes = new string[] { "foo1", "foo2" }
        });
        await _userConsent.StoreUserConsentAsync(new Consent()
        {
            CreationTime = DateTime.UtcNow,
            ClientId = "client2",
            SubjectId = "123",
            Scopes = new string[] { "foo3" }
        });
        await _userConsent.StoreUserConsentAsync(new Consent()
        {
            CreationTime = DateTime.UtcNow,
            ClientId = "client1",
            SubjectId = "456",
            Scopes = new string[] { "foo3" }
        });

        var handle1 = await _referenceTokens.StoreReferenceTokenAsync(new Token()
        {
            ClientId = "client1",
            Audiences = { "aud" },
            CreationTime = DateTime.UtcNow,
            Type = "type",
            Claims = new List<Claim>
                {
                    new Claim("sub", "123"),
                    new Claim("scope", "bar1"),
                    new Claim("scope", "bar2")
                }
        });

        var handle2 = await _referenceTokens.StoreReferenceTokenAsync(new Token()
        {
            ClientId = "client2",
            Audiences = { "aud" },
            CreationTime = DateTime.UtcNow,
            Type = "type",
            Claims = new List<Claim>
                {
                    new Claim("sub", "123"),
                    new Claim("scope", "bar3")
                }
        });

        var handle3 = await _referenceTokens.StoreReferenceTokenAsync(new Token()
        {
            ClientId = "client1",
            Audiences = { "aud" },
            CreationTime = DateTime.UtcNow,
            Type = "type",
            Claims = new List<Claim>
                {
                    new Claim("sub", "456"),
                    new Claim("scope", "bar3")
                }
        });

        var handle4 = await _refreshTokens.StoreRefreshTokenAsync(new RefreshToken()
        {
            CreationTime = DateTime.UtcNow,
            Lifetime = 10,
            AccessToken = new Token
            {
                ClientId = "client1",
                Audiences = { "aud" },
                CreationTime = DateTime.UtcNow,
                Type = "type",
                Claims = new List<Claim>
                    {
                        new Claim("sub", "123"),
                        new Claim("scope", "baz1"),
                        new Claim("scope", "baz2")
                    }
            },
            Version = 1
        });
        var handle5 = await _refreshTokens.StoreRefreshTokenAsync(new RefreshToken()
        {
            CreationTime = DateTime.UtcNow,
            Lifetime = 10,
            AccessToken = new Token
            {
                ClientId = "client1",
                Audiences = { "aud" },
                CreationTime = DateTime.UtcNow,
                Type = "type",
                Claims = new List<Claim>
                    {
                        new Claim("sub", "456"),
                        new Claim("scope", "baz3")
                    }
            },
            Version = 1
        });
        var handle6 = await _refreshTokens.StoreRefreshTokenAsync(new RefreshToken()
        {
            CreationTime = DateTime.UtcNow,
            Lifetime = 10,
            AccessToken = new Token
            {
                ClientId = "client2",
                Audiences = { "aud" },
                CreationTime = DateTime.UtcNow,
                Type = "type",
                Claims = new List<Claim>
                    {
                        new Claim("sub", "123"),
                        new Claim("scope", "baz3")
                    }
            },
            Version = 1
        });

        var handle7 = await _codes.StoreAuthorizationCodeAsync(new AuthorizationCode()
        {
            ClientId = "client1",
            CreationTime = DateTime.UtcNow,
            Lifetime = 10,
            Subject = _user,
            CodeChallenge = "challenge",
            RedirectUri = "http://client/cb",
            Nonce = "nonce",
            RequestedScopes = new string[] { "quux1", "quux2" }
        });

        var handle8 = await _codes.StoreAuthorizationCodeAsync(new AuthorizationCode()
        {
            ClientId = "client2",
            CreationTime = DateTime.UtcNow,
            Lifetime = 10,
            Subject = _user,
            CodeChallenge = "challenge",
            RedirectUri = "http://client/cb",
            Nonce = "nonce",
            RequestedScopes = new string[] { "quux3" }
        });

        var handle9 = await _codes.StoreAuthorizationCodeAsync(new AuthorizationCode()
        {
            ClientId = "client1",
            CreationTime = DateTime.UtcNow,
            Lifetime = 10,
            Subject = new IdentityServerUser("456").CreatePrincipal(),
            CodeChallenge = "challenge",
            RedirectUri = "http://client/cb",
            Nonce = "nonce",
            RequestedScopes = new string[] { "quux3" }
        });

        var grants = await _subject.GetAllGrantsAsync("123");

        Assert.Equal(2, grants.Count());
        var grant1 = grants.First(x => x.ClientId == "client1");
        Assert.Equal("123", grant1.SubjectId);
        Assert.Equal("client1", grant1.ClientId);
        Assert.Equivalent(new string[] { "foo1", "foo2", "bar1", "bar2", "baz1", "baz2", "quux1", "quux2" }, grant1.Scopes, strict: true);

        var grant2 = grants.First(x => x.ClientId == "client2");
        Assert.Equal("123", grant2.SubjectId);
        Assert.Equal("client2", grant2.ClientId);
        Assert.Equivalent(new string[] { "foo3", "bar3", "baz3", "quux3" }, grant2.Scopes, strict: true);
    }

    [Fact]
    public async Task RemoveAllGrantsAsync_should_remove_all_grants()
    {
        await _userConsent.StoreUserConsentAsync(new Consent()
        {
            ClientId = "client1",
            SubjectId = "123",
            Scopes = new string[] { "foo1", "foo2" }
        });
        await _userConsent.StoreUserConsentAsync(new Consent()
        {
            ClientId = "client2",
            SubjectId = "123",
            Scopes = new string[] { "foo3" }
        });
        await _userConsent.StoreUserConsentAsync(new Consent()
        {
            ClientId = "client1",
            SubjectId = "456",
            Scopes = new string[] { "foo3" }
        });

        var handle1 = await _referenceTokens.StoreReferenceTokenAsync(new Token()
        {
            ClientId = "client1",
            Audiences = { "aud" },
            CreationTime = DateTime.UtcNow,
            Lifetime = 10,
            Type = "type",
            Claims = new List<Claim>
                {
                    new Claim("sub", "123"),
                    new Claim("scope", "bar1"),
                    new Claim("scope", "bar2")
                }
        });

        var handle2 = await _referenceTokens.StoreReferenceTokenAsync(new Token()
        {
            ClientId = "client2",
            Audiences = { "aud" },
            CreationTime = DateTime.UtcNow,
            Lifetime = 10,
            Type = "type",
            Claims = new List<Claim>
                {
                    new Claim("sub", "123"),
                    new Claim("scope", "bar3")
                }
        });

        var handle3 = await _referenceTokens.StoreReferenceTokenAsync(new Token()
        {
            ClientId = "client1",
            Audiences = { "aud" },
            CreationTime = DateTime.UtcNow,
            Lifetime = 10,
            Type = "type",
            Claims = new List<Claim>
                {
                    new Claim("sub", "456"),
                    new Claim("scope", "bar3")
                }
        });

        var handle4 = await _refreshTokens.StoreRefreshTokenAsync(new RefreshToken()
        {
            CreationTime = DateTime.UtcNow,
            Lifetime = 10,
            AccessToken = new Token
            {
                ClientId = "client1",
                Audiences = { "aud" },
                CreationTime = DateTime.UtcNow,
                Type = "type",
                Claims = new List<Claim>
                    {
                        new Claim("sub", "123"),
                        new Claim("scope", "baz1"),
                        new Claim("scope", "baz2")
                    }
            },
            Version = 1
        });
        var handle5 = await _refreshTokens.StoreRefreshTokenAsync(new RefreshToken()
        {
            CreationTime = DateTime.UtcNow,
            Lifetime = 10,
            AccessToken = new Token
            {
                ClientId = "client1",
                Audiences = { "aud" },
                CreationTime = DateTime.UtcNow,
                Type = "type",
                Claims = new List<Claim>
                    {
                        new Claim("sub", "456"),
                        new Claim("scope", "baz3")
                    }
            },
            Version = 1
        });
        var handle6 = await _refreshTokens.StoreRefreshTokenAsync(new RefreshToken()
        {
            CreationTime = DateTime.UtcNow,
            Lifetime = 10,
            AccessToken = new Token
            {
                ClientId = "client2",
                Audiences = { "aud" },
                CreationTime = DateTime.UtcNow,
                Type = "type",
                Claims = new List<Claim>
                    {
                        new Claim("sub", "123"),
                        new Claim("scope", "baz3")
                    }
            },
            Version = 1
        });

        var handle7 = await _codes.StoreAuthorizationCodeAsync(new AuthorizationCode()
        {
            ClientId = "client1",
            CreationTime = DateTime.UtcNow,
            Lifetime = 10,
            Subject = _user,
            CodeChallenge = "challenge",
            RedirectUri = "http://client/cb",
            Nonce = "nonce",
            RequestedScopes = new string[] { "quux1", "quux2" }
        });

        var handle8 = await _codes.StoreAuthorizationCodeAsync(new AuthorizationCode()
        {
            ClientId = "client2",
            CreationTime = DateTime.UtcNow,
            Lifetime = 10,
            Subject = _user,
            CodeChallenge = "challenge",
            RedirectUri = "http://client/cb",
            Nonce = "nonce",
            RequestedScopes = new string[] { "quux3" }
        });

        var handle9 = await _codes.StoreAuthorizationCodeAsync(new AuthorizationCode()
        {
            ClientId = "client1",
            CreationTime = DateTime.UtcNow,
            Lifetime = 10,
            Subject = new IdentityServerUser("456").CreatePrincipal(),
            CodeChallenge = "challenge",
            RedirectUri = "http://client/cb",
            Nonce = "nonce",
            RequestedScopes = new string[] { "quux3" }
        });

        await _subject.RemoveAllGrantsAsync("123", "client1");

        Assert.Null((await _referenceTokens.GetReferenceTokenAsync(handle1)));
        Assert.NotNull((await _referenceTokens.GetReferenceTokenAsync(handle2)));
        Assert.NotNull((await _referenceTokens.GetReferenceTokenAsync(handle3)));
        Assert.Null((await _refreshTokens.GetRefreshTokenAsync(handle4)));
        Assert.NotNull((await _refreshTokens.GetRefreshTokenAsync(handle5)));
        Assert.NotNull((await _refreshTokens.GetRefreshTokenAsync(handle6)));
        Assert.Null((await _codes.GetAuthorizationCodeAsync(handle7)));
        Assert.NotNull((await _codes.GetAuthorizationCodeAsync(handle8)));
        Assert.NotNull((await _codes.GetAuthorizationCodeAsync(handle9)));
    }
    [Fact]
    public async Task RemoveAllGrantsAsync_should_filter_on_session_id()
    {
        {
            var handle1 = await _refreshTokens.StoreRefreshTokenAsync(new RefreshToken()
            {
                CreationTime = DateTime.UtcNow,
                Lifetime = 10,
                AccessToken = new Token
                {
                    ClientId = "client1",
                    Audiences = { "aud" },
                    CreationTime = DateTime.UtcNow,
                    Type = "type",
                    Claims = new List<Claim>
                        {
                            new Claim("sub", "123"),
                            new Claim("sid", "session1"),
                            new Claim("scope", "baz")
                        }
                },
                Version = 1
            });
            var handle2 = await _refreshTokens.StoreRefreshTokenAsync(new RefreshToken()
            {
                CreationTime = DateTime.UtcNow,
                Lifetime = 10,
                AccessToken = new Token
                {
                    ClientId = "client2",
                    Audiences = { "aud" },
                    CreationTime = DateTime.UtcNow,
                    Type = "type",
                    Claims = new List<Claim>
                        {
                            new Claim("sub", "123"),
                            new Claim("sid", "session1"),
                            new Claim("scope", "baz")
                        }
                },
                Version = 1
            });
            var handle3 = await _refreshTokens.StoreRefreshTokenAsync(new RefreshToken()
            {
                CreationTime = DateTime.UtcNow,
                Lifetime = 10,
                AccessToken = new Token
                {
                    ClientId = "client3",
                    Audiences = { "aud" },
                    CreationTime = DateTime.UtcNow,
                    Type = "type",
                    Claims = new List<Claim>
                        {
                            new Claim("sub", "123"),
                            new Claim("sid", "session3"),
                            new Claim("scope", "baz")
                        }
                },
                Version = 1
            });

            await _subject.RemoveAllGrantsAsync("123");

            Assert.Null((await _refreshTokens.GetRefreshTokenAsync(handle1)));
            Assert.Null((await _refreshTokens.GetRefreshTokenAsync(handle2)));
            Assert.Null((await _refreshTokens.GetRefreshTokenAsync(handle3)));
            await _refreshTokens.RemoveRefreshTokenAsync(handle1);
            await _refreshTokens.RemoveRefreshTokenAsync(handle2);
            await _refreshTokens.RemoveRefreshTokenAsync(handle3);
        }
        {
            var handle1 = await _refreshTokens.StoreRefreshTokenAsync(new RefreshToken()
            {
                CreationTime = DateTime.UtcNow,
                Lifetime = 10,
                AccessToken = new Token
                {
                    ClientId = "client1",
                    Audiences = { "aud" },
                    CreationTime = DateTime.UtcNow,
                    Type = "type",
                    Claims = new List<Claim>
                        {
                            new Claim("sub", "123"),
                            new Claim("sid", "session1"),
                            new Claim("scope", "baz")
                        }
                },
                Version = 1
            });
            var handle2 = await _refreshTokens.StoreRefreshTokenAsync(new RefreshToken()
            {
                CreationTime = DateTime.UtcNow,
                Lifetime = 10,
                AccessToken = new Token
                {
                    ClientId = "client2",
                    Audiences = { "aud" },
                    CreationTime = DateTime.UtcNow,
                    Type = "type",
                    Claims = new List<Claim>
                        {
                            new Claim("sub", "123"),
                            new Claim("sid", "session1"),
                            new Claim("scope", "baz")
                        }
                },
                Version = 1
            });
            var handle3 = await _refreshTokens.StoreRefreshTokenAsync(new RefreshToken()
            {
                CreationTime = DateTime.UtcNow,
                Lifetime = 10,
                AccessToken = new Token
                {
                    ClientId = "client3",
                    Audiences = { "aud" },
                    CreationTime = DateTime.UtcNow,
                    Type = "type",
                    Claims = new List<Claim>
                        {
                            new Claim("sub", "123"),
                            new Claim("sid", "session3"),
                            new Claim("scope", "baz")
                        }
                },
                Version = 1
            });

            await _subject.RemoveAllGrantsAsync("123", "client1");

            Assert.Null((await _refreshTokens.GetRefreshTokenAsync(handle1)));
            Assert.NotNull((await _refreshTokens.GetRefreshTokenAsync(handle2)));
            Assert.NotNull((await _refreshTokens.GetRefreshTokenAsync(handle3)));
            await _refreshTokens.RemoveRefreshTokenAsync(handle1);
            await _refreshTokens.RemoveRefreshTokenAsync(handle2);
            await _refreshTokens.RemoveRefreshTokenAsync(handle3);
        }
        {
            var handle1 = await _refreshTokens.StoreRefreshTokenAsync(new RefreshToken()
            {
                CreationTime = DateTime.UtcNow,
                Lifetime = 10,
                AccessToken = new Token
                {
                    ClientId = "client1",
                    Audiences = { "aud" },
                    CreationTime = DateTime.UtcNow,
                    Type = "type",
                    Claims = new List<Claim>
                        {
                            new Claim("sub", "123"),
                            new Claim("sid", "session1"),
                            new Claim("scope", "baz")
                        }
                },
                Version = 1
            });
            var handle2 = await _refreshTokens.StoreRefreshTokenAsync(new RefreshToken()
            {
                CreationTime = DateTime.UtcNow,
                Lifetime = 10,
                AccessToken = new Token
                {
                    ClientId = "client2",
                    Audiences = { "aud" },
                    CreationTime = DateTime.UtcNow,
                    Type = "type",
                    Claims = new List<Claim>
                        {
                            new Claim("sub", "123"),
                            new Claim("sid", "session1"),
                            new Claim("scope", "baz")
                        }
                },
                Version = 1
            });
            var handle3 = await _refreshTokens.StoreRefreshTokenAsync(new RefreshToken()
            {
                CreationTime = DateTime.UtcNow,
                Lifetime = 10,
                AccessToken = new Token
                {
                    ClientId = "client3",
                    Audiences = { "aud" },
                    CreationTime = DateTime.UtcNow,
                    Type = "type",
                    Claims = new List<Claim>
                        {
                            new Claim("sub", "123"),
                            new Claim("sid", "session1"),
                            new Claim("scope", "baz")
                        }
                },
                Version = 1
            });
            var handle4 = await _refreshTokens.StoreRefreshTokenAsync(new RefreshToken()
            {
                CreationTime = DateTime.UtcNow,
                Lifetime = 10,
                AccessToken = new Token
                {
                    ClientId = "client1",
                    Audiences = { "aud" },
                    CreationTime = DateTime.UtcNow,
                    Type = "type",
                    Claims = new List<Claim>
                        {
                            new Claim("sub", "123"),
                            new Claim("sid", "session2"),
                            new Claim("scope", "baz")
                        }
                },
                Version = 1
            });
            await _subject.RemoveAllGrantsAsync("123", "client1", "session1");

            Assert.Null((await _refreshTokens.GetRefreshTokenAsync(handle1)));
            Assert.NotNull((await _refreshTokens.GetRefreshTokenAsync(handle2)));
            Assert.NotNull((await _refreshTokens.GetRefreshTokenAsync(handle3)));
            Assert.NotNull((await _refreshTokens.GetRefreshTokenAsync(handle4)));
            await _refreshTokens.RemoveRefreshTokenAsync(handle1);
            await _refreshTokens.RemoveRefreshTokenAsync(handle2);
            await _refreshTokens.RemoveRefreshTokenAsync(handle3);
            await _refreshTokens.RemoveRefreshTokenAsync(handle4);
        }
        {
            var handle1 = await _refreshTokens.StoreRefreshTokenAsync(new RefreshToken()
            {
                CreationTime = DateTime.UtcNow,
                Lifetime = 10,
                AccessToken = new Token
                {
                    ClientId = "client1",
                    Audiences = { "aud" },
                    CreationTime = DateTime.UtcNow,
                    Type = "type",
                    Claims = new List<Claim>
                        {
                            new Claim("sub", "123"),
                            new Claim("sid", "session1"),
                            new Claim("scope", "baz")
                        }
                },
                Version = 1
            });
            var handle2 = await _refreshTokens.StoreRefreshTokenAsync(new RefreshToken()
            {
                CreationTime = DateTime.UtcNow,
                Lifetime = 10,
                AccessToken = new Token
                {
                    ClientId = "client2",
                    Audiences = { "aud" },
                    CreationTime = DateTime.UtcNow,
                    Type = "type",
                    Claims = new List<Claim>
                        {
                            new Claim("sub", "123"),
                            new Claim("sid", "session1"),
                            new Claim("scope", "baz")
                        }
                },
                Version = 1
            });
            var handle3 = await _refreshTokens.StoreRefreshTokenAsync(new RefreshToken()
            {
                CreationTime = DateTime.UtcNow,
                Lifetime = 10,
                AccessToken = new Token
                {
                    ClientId = "client3",
                    Audiences = { "aud" },
                    CreationTime = DateTime.UtcNow,
                    Type = "type",
                    Claims = new List<Claim>
                        {
                            new Claim("sub", "123"),
                            new Claim("sid", "session1"),
                            new Claim("scope", "baz")
                        }
                },
                Version = 1
            });
            var handle4 = await _refreshTokens.StoreRefreshTokenAsync(new RefreshToken()
            {
                CreationTime = DateTime.UtcNow,
                Lifetime = 10,
                AccessToken = new Token
                {
                    ClientId = "client1",
                    Audiences = { "aud" },
                    CreationTime = DateTime.UtcNow,
                    Type = "type",
                    Claims = new List<Claim>
                        {
                            new Claim("sub", "123"),
                            new Claim("sid", "session2"),
                            new Claim("scope", "baz")
                        }
                },
                Version = 1
            });
            await _subject.RemoveAllGrantsAsync("123", sessionId: "session1");

            Assert.Null((await _refreshTokens.GetRefreshTokenAsync(handle1)));
            Assert.Null((await _refreshTokens.GetRefreshTokenAsync(handle2)));
            Assert.Null((await _refreshTokens.GetRefreshTokenAsync(handle3)));
            Assert.NotNull((await _refreshTokens.GetRefreshTokenAsync(handle4)));
            await _refreshTokens.RemoveRefreshTokenAsync(handle1);
            await _refreshTokens.RemoveRefreshTokenAsync(handle2);
            await _refreshTokens.RemoveRefreshTokenAsync(handle3);
            await _refreshTokens.RemoveRefreshTokenAsync(handle4);
        }
    }

    [Fact]
    public async Task GetAllGrantsAsync_should_aggregate_correctly()
    {
        await _userConsent.StoreUserConsentAsync(new Consent()
        {
            ClientId = "client1",
            SubjectId = "123",
            Scopes = new string[] { "foo1", "foo2" }
        });

        var grants = await _subject.GetAllGrantsAsync("123");

        Assert.Single(grants);
        Assert.All(new string[] { "foo1", "foo2" }, expected => Assert.Contains(expected, grants.First().Scopes));

        var handle9 = await _codes.StoreAuthorizationCodeAsync(new AuthorizationCode()
        {
            ClientId = "client1",
            CreationTime = DateTime.UtcNow,
            Lifetime = 10,
            Subject = new IdentityServerUser("123").CreatePrincipal(),
            CodeChallenge = "challenge",
            RedirectUri = "http://client/cb",
            Nonce = "nonce",
            RequestedScopes = new string[] { "quux3" }
        });

        grants = await _subject.GetAllGrantsAsync("123");

        Assert.Single(grants);
        Assert.All(new string[] { "foo1", "foo2", "quux3" }, expected => Assert.Contains(expected, grants.First().Scopes));
    }
}
