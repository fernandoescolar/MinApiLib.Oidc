namespace IdentityServer.UnitTests.Stores;

public class InMemoryPersistedGrantStoreTests
{
    InMemoryPersistedGrantStore _subject;

    public InMemoryPersistedGrantStoreTests()
    {
        _subject = new InMemoryPersistedGrantStore();
    }

    [Fact]
    public async Task Store_should_persist_value()
    {
        {
            var item = await _subject.GetAsync("key1");
            Assert.Null(item);
        }

        await _subject.StoreAsync(new PersistedGrant() { Key = "key1" });

        {
            var item = await _subject.GetAsync("key1");
            Assert.NotNull(item);
        }
    }

    [Fact]
    public async Task GetAll_should_filter()
    {
        await _subject.StoreAsync(new PersistedGrant() { Key = "key1", SubjectId = "sub1", ClientId = "client1", SessionId = "session1" });
        await _subject.StoreAsync(new PersistedGrant() { Key = "key2", SubjectId = "sub1", ClientId = "client2", SessionId = "session1" });
        await _subject.StoreAsync(new PersistedGrant() { Key = "key3", SubjectId = "sub1", ClientId = "client1", SessionId = "session2" });
        await _subject.StoreAsync(new PersistedGrant() { Key = "key4", SubjectId = "sub1", ClientId = "client3", SessionId = "session2" });
        await _subject.StoreAsync(new PersistedGrant() { Key = "key5", SubjectId = "sub1", ClientId = "client4", SessionId = "session3" });
        await _subject.StoreAsync(new PersistedGrant() { Key = "key6", SubjectId = "sub1", ClientId = "client4", SessionId = "session4" });

        await _subject.StoreAsync(new PersistedGrant() { Key = "key7", SubjectId = "sub2", ClientId = "client4", SessionId = "session4" });


        Assert.Equivalent(new[] { "key1", "key2", "key3", "key4", "key5", "key6" }, (await _subject.GetAllAsync(new PersistedGrantFilter
        {
            SubjectId = "sub1"
        }))
        .Select(x => x.Key), strict: true);

        Assert.Equivalent(new[] { "key7" }, (await _subject.GetAllAsync(new PersistedGrantFilter
        {
            SubjectId = "sub2"
        }))
        .Select(x => x.Key), strict: true);

        Assert.Empty((await _subject.GetAllAsync(new PersistedGrantFilter
        {
            SubjectId = "sub3"
        }))
        .Select(x => x.Key));

        Assert.Equivalent(new[] { "key1", "key3" }, (await _subject.GetAllAsync(new PersistedGrantFilter
        {
            SubjectId = "sub1",
            ClientId = "client1"
        }))
        .Select(x => x.Key), strict: true);

        Assert.Equivalent(new[] { "key2" }, (await _subject.GetAllAsync(new PersistedGrantFilter
        {
            SubjectId = "sub1",
            ClientId = "client2"
        }))
        .Select(x => x.Key), strict: true);

        Assert.Equivalent(new[] { "key4" }, (await _subject.GetAllAsync(new PersistedGrantFilter
        {
            SubjectId = "sub1",
            ClientId = "client3"
        }))
        .Select(x => x.Key), strict: true);

        Assert.Equivalent(new[] { "key5", "key6" }, (await _subject.GetAllAsync(new PersistedGrantFilter
        {
            SubjectId = "sub1",
            ClientId = "client4"
        }))
        .Select(x => x.Key), strict: true);

        Assert.Empty((await _subject.GetAllAsync(new PersistedGrantFilter
        {
            SubjectId = "sub1",
            ClientId = "client5"
        }))
        .Select(x => x.Key));

        Assert.Empty((await _subject.GetAllAsync(new PersistedGrantFilter
        {
            SubjectId = "sub2",
            ClientId = "client1"
        }))
        .Select(x => x.Key));

        Assert.Equivalent(new[] { "key7" }, (await _subject.GetAllAsync(new PersistedGrantFilter
        {
            SubjectId = "sub2",
            ClientId = "client4"
        }))
        .Select(x => x.Key), strict: true);

        Assert.Empty((await _subject.GetAllAsync(new PersistedGrantFilter
        {
            SubjectId = "sub3",
            ClientId = "client1"
        }))
        .Select(x => x.Key));

        Assert.Equivalent(new[] { "key1" }, (await _subject.GetAllAsync(new PersistedGrantFilter
        {
            SubjectId = "sub1",
            ClientId = "client1",
            SessionId = "session1"
        }))
        .Select(x => x.Key), strict: true);

        Assert.Equivalent(new[] { "key3" }, (await _subject.GetAllAsync(new PersistedGrantFilter
        {
            SubjectId = "sub1",
            ClientId = "client1",
            SessionId = "session2"
        }))
        .Select(x => x.Key), strict: true);

        Assert.Empty((await _subject.GetAllAsync(new PersistedGrantFilter
        {
            SubjectId = "sub1",
            ClientId = "client1",
            SessionId = "session3"
        }))
        .Select(x => x.Key));

        Assert.Equivalent(new[] { "key2" }, (await _subject.GetAllAsync(new PersistedGrantFilter
        {
            SubjectId = "sub1",
            ClientId = "client2",
            SessionId = "session1"
        }))
        .Select(x => x.Key), strict: true);

        Assert.Empty((await _subject.GetAllAsync(new PersistedGrantFilter
        {
            SubjectId = "sub1",
            ClientId = "client2",
            SessionId = "session2"
        }))
        .Select(x => x.Key));

        Assert.Equivalent(new[] { "key6" }, (await _subject.GetAllAsync(new PersistedGrantFilter
        {
            SubjectId = "sub1",
            ClientId = "client4",
            SessionId = "session4"
        }))
        .Select(x => x.Key), strict: true);

        Assert.Equivalent(new[] { "key7" }, (await _subject.GetAllAsync(new PersistedGrantFilter
        {
            SubjectId = "sub2",
            ClientId = "client4",
            SessionId = "session4"
        }))
        .Select(x => x.Key), strict: true);

        Assert.Empty((await _subject.GetAllAsync(new PersistedGrantFilter
        {
            SubjectId = "sub2",
            ClientId = "client4",
            SessionId = "session1"
        }))
        .Select(x => x.Key));

        Assert.Empty((await _subject.GetAllAsync(new PersistedGrantFilter
        {
            SubjectId = "sub2",
            ClientId = "client4",
            SessionId = "session5"
        }))
        .Select(x => x.Key));
    }

    [Fact]
    public async Task RemoveAll_should_filter()
    {
        {
            await Populate();
            await _subject.RemoveAllAsync(new PersistedGrantFilter
            {
                SubjectId = "sub1"
            });
            Assert.Null((await _subject.GetAsync("key1")));
            Assert.Null((await _subject.GetAsync("key2")));
            Assert.Null((await _subject.GetAsync("key3")));
            Assert.Null((await _subject.GetAsync("key4")));
            Assert.Null((await _subject.GetAsync("key5")));
            Assert.Null((await _subject.GetAsync("key6")));
            Assert.NotNull((await _subject.GetAsync("key7")));
        }
        {
            await Populate();
            await _subject.RemoveAllAsync(new PersistedGrantFilter
            {
                SubjectId = "sub2"
            });
            Assert.NotNull((await _subject.GetAsync("key1")));
            Assert.NotNull((await _subject.GetAsync("key2")));
            Assert.NotNull((await _subject.GetAsync("key3")));
            Assert.NotNull((await _subject.GetAsync("key4")));
            Assert.NotNull((await _subject.GetAsync("key5")));
            Assert.NotNull((await _subject.GetAsync("key6")));
            Assert.Null((await _subject.GetAsync("key7")));
        }
        {
            await Populate();
            await _subject.RemoveAllAsync(new PersistedGrantFilter
            {
                SubjectId = "sub3"
            });
            Assert.NotNull((await _subject.GetAsync("key1")));
            Assert.NotNull((await _subject.GetAsync("key2")));
            Assert.NotNull((await _subject.GetAsync("key3")));
            Assert.NotNull((await _subject.GetAsync("key4")));
            Assert.NotNull((await _subject.GetAsync("key5")));
            Assert.NotNull((await _subject.GetAsync("key6")));
            Assert.NotNull((await _subject.GetAsync("key7")));
        }
        {
            await Populate();
            await _subject.RemoveAllAsync(new PersistedGrantFilter
            {
                SubjectId = "sub1",
                ClientId = "client1"
            });
            Assert.Null((await _subject.GetAsync("key1")));
            Assert.NotNull((await _subject.GetAsync("key2")));
            Assert.Null((await _subject.GetAsync("key3")));
            Assert.NotNull((await _subject.GetAsync("key4")));
            Assert.NotNull((await _subject.GetAsync("key5")));
            Assert.NotNull((await _subject.GetAsync("key6")));
            Assert.NotNull((await _subject.GetAsync("key7")));
        }
        {
            await Populate();
            await _subject.RemoveAllAsync(new PersistedGrantFilter
            {
                SubjectId = "sub1",
                ClientId = "client2"
            });
            Assert.NotNull((await _subject.GetAsync("key1")));
            Assert.Null((await _subject.GetAsync("key2")));
            Assert.NotNull((await _subject.GetAsync("key3")));
            Assert.NotNull((await _subject.GetAsync("key4")));
            Assert.NotNull((await _subject.GetAsync("key5")));
            Assert.NotNull((await _subject.GetAsync("key6")));
            Assert.NotNull((await _subject.GetAsync("key7")));
        }
        {
            await Populate();
            await _subject.RemoveAllAsync(new PersistedGrantFilter
            {
                SubjectId = "sub1",
                ClientId = "client3"
            });
            Assert.NotNull((await _subject.GetAsync("key1")));
            Assert.NotNull((await _subject.GetAsync("key2")));
            Assert.NotNull((await _subject.GetAsync("key3")));
            Assert.Null((await _subject.GetAsync("key4")));
            Assert.NotNull((await _subject.GetAsync("key5")));
            Assert.NotNull((await _subject.GetAsync("key6")));
            Assert.NotNull((await _subject.GetAsync("key7")));
        }
        {
            await Populate();
            await _subject.RemoveAllAsync(new PersistedGrantFilter
            {
                SubjectId = "sub1",
                ClientId = "client4"
            });
            Assert.NotNull((await _subject.GetAsync("key1")));
            Assert.NotNull((await _subject.GetAsync("key2")));
            Assert.NotNull((await _subject.GetAsync("key3")));
            Assert.NotNull((await _subject.GetAsync("key4")));
            Assert.Null((await _subject.GetAsync("key5")));
            Assert.Null((await _subject.GetAsync("key6")));
            Assert.NotNull((await _subject.GetAsync("key7")));
        }
        {
            await Populate();
            await _subject.RemoveAllAsync(new PersistedGrantFilter
            {
                SubjectId = "sub1",
                ClientId = "client5"
            });
            Assert.NotNull((await _subject.GetAsync("key1")));
            Assert.NotNull((await _subject.GetAsync("key2")));
            Assert.NotNull((await _subject.GetAsync("key3")));
            Assert.NotNull((await _subject.GetAsync("key4")));
            Assert.NotNull((await _subject.GetAsync("key5")));
            Assert.NotNull((await _subject.GetAsync("key6")));
            Assert.NotNull((await _subject.GetAsync("key7")));
        }
        {
            await Populate();
            await _subject.RemoveAllAsync(new PersistedGrantFilter
            {
                SubjectId = "sub2",
                ClientId = "client1"
            });
            Assert.NotNull((await _subject.GetAsync("key1")));
            Assert.NotNull((await _subject.GetAsync("key2")));
            Assert.NotNull((await _subject.GetAsync("key3")));
            Assert.NotNull((await _subject.GetAsync("key4")));
            Assert.NotNull((await _subject.GetAsync("key5")));
            Assert.NotNull((await _subject.GetAsync("key6")));
            Assert.NotNull((await _subject.GetAsync("key7")));
        }
        {
            await Populate();
            await _subject.RemoveAllAsync(new PersistedGrantFilter
            {
                SubjectId = "sub1",
                ClientId = "client4"
            });
            Assert.NotNull((await _subject.GetAsync("key1")));
            Assert.NotNull((await _subject.GetAsync("key2")));
            Assert.NotNull((await _subject.GetAsync("key3")));
            Assert.NotNull((await _subject.GetAsync("key4")));
            Assert.Null((await _subject.GetAsync("key5")));
            Assert.Null((await _subject.GetAsync("key6")));
            Assert.NotNull((await _subject.GetAsync("key7")));
        }
        {
            await Populate();
            await _subject.RemoveAllAsync(new PersistedGrantFilter
            {
                SubjectId = "sub3",
                ClientId = "client1"
            });
            Assert.NotNull((await _subject.GetAsync("key1")));
            Assert.NotNull((await _subject.GetAsync("key2")));
            Assert.NotNull((await _subject.GetAsync("key3")));
            Assert.NotNull((await _subject.GetAsync("key4")));
            Assert.NotNull((await _subject.GetAsync("key5")));
            Assert.NotNull((await _subject.GetAsync("key6")));
            Assert.NotNull((await _subject.GetAsync("key7")));
        }
        {
            await Populate();
            await _subject.RemoveAllAsync(new PersistedGrantFilter
            {
                SubjectId = "sub1",
                ClientId = "client1",
                SessionId = "session1"
            });
            Assert.Null((await _subject.GetAsync("key1")));
            Assert.NotNull((await _subject.GetAsync("key2")));
            Assert.NotNull((await _subject.GetAsync("key3")));
            Assert.NotNull((await _subject.GetAsync("key4")));
            Assert.NotNull((await _subject.GetAsync("key5")));
            Assert.NotNull((await _subject.GetAsync("key6")));
            Assert.NotNull((await _subject.GetAsync("key7")));
        }
        {
            await Populate();
            await _subject.RemoveAllAsync(new PersistedGrantFilter
            {
                SubjectId = "sub1",
                ClientId = "client1",
                SessionId = "session2"
            });
            Assert.NotNull((await _subject.GetAsync("key1")));
            Assert.NotNull((await _subject.GetAsync("key2")));
            Assert.Null((await _subject.GetAsync("key3")));
            Assert.NotNull((await _subject.GetAsync("key4")));
            Assert.NotNull((await _subject.GetAsync("key5")));
            Assert.NotNull((await _subject.GetAsync("key6")));
            Assert.NotNull((await _subject.GetAsync("key7")));
        }
        {
            await Populate();
            await _subject.RemoveAllAsync(new PersistedGrantFilter
            {
                SubjectId = "sub1",
                ClientId = "client1",
                SessionId = "session3"
            });
            Assert.NotNull((await _subject.GetAsync("key1")));
            Assert.NotNull((await _subject.GetAsync("key2")));
            Assert.NotNull((await _subject.GetAsync("key3")));
            Assert.NotNull((await _subject.GetAsync("key4")));
            Assert.NotNull((await _subject.GetAsync("key5")));
            Assert.NotNull((await _subject.GetAsync("key6")));
            Assert.NotNull((await _subject.GetAsync("key7")));
        }
        {
            await Populate();
            await _subject.RemoveAllAsync(new PersistedGrantFilter
            {
                SubjectId = "sub1",
                ClientId = "client2",
                SessionId = "session1"
            });
            Assert.NotNull((await _subject.GetAsync("key1")));
            Assert.Null((await _subject.GetAsync("key2")));
            Assert.NotNull((await _subject.GetAsync("key3")));
            Assert.NotNull((await _subject.GetAsync("key4")));
            Assert.NotNull((await _subject.GetAsync("key5")));
            Assert.NotNull((await _subject.GetAsync("key6")));
            Assert.NotNull((await _subject.GetAsync("key7")));
        }
        {
            await Populate();
            await _subject.RemoveAllAsync(new PersistedGrantFilter
            {
                SubjectId = "sub1",
                ClientId = "client2",
                SessionId = "session2"
            });
            Assert.NotNull((await _subject.GetAsync("key1")));
            Assert.NotNull((await _subject.GetAsync("key2")));
            Assert.NotNull((await _subject.GetAsync("key3")));
            Assert.NotNull((await _subject.GetAsync("key4")));
            Assert.NotNull((await _subject.GetAsync("key5")));
            Assert.NotNull((await _subject.GetAsync("key6")));
            Assert.NotNull((await _subject.GetAsync("key7")));
        }
        {
            await Populate();
            await _subject.RemoveAllAsync(new PersistedGrantFilter
            {
                SubjectId = "sub1",
                ClientId = "client4",
                SessionId = "session4"
            });
            Assert.NotNull((await _subject.GetAsync("key1")));
            Assert.NotNull((await _subject.GetAsync("key2")));
            Assert.NotNull((await _subject.GetAsync("key3")));
            Assert.NotNull((await _subject.GetAsync("key4")));
            Assert.NotNull((await _subject.GetAsync("key5")));
            Assert.Null((await _subject.GetAsync("key6")));
            Assert.NotNull((await _subject.GetAsync("key7")));
        }
        {
            await Populate();
            await _subject.RemoveAllAsync(new PersistedGrantFilter
            {
                SubjectId = "sub2",
                ClientId = "client4",
                SessionId = "session4"
            });
            Assert.NotNull((await _subject.GetAsync("key1")));
            Assert.NotNull((await _subject.GetAsync("key2")));
            Assert.NotNull((await _subject.GetAsync("key3")));
            Assert.NotNull((await _subject.GetAsync("key4")));
            Assert.NotNull((await _subject.GetAsync("key5")));
            Assert.NotNull((await _subject.GetAsync("key6")));
            Assert.Null((await _subject.GetAsync("key7")));
        }
        {
            await Populate();
            await _subject.RemoveAllAsync(new PersistedGrantFilter
            {
                SubjectId = "sub2",
                ClientId = "client4",
                SessionId = "session1"
            });
            Assert.NotNull((await _subject.GetAsync("key1")));
            Assert.NotNull((await _subject.GetAsync("key2")));
            Assert.NotNull((await _subject.GetAsync("key3")));
            Assert.NotNull((await _subject.GetAsync("key4")));
            Assert.NotNull((await _subject.GetAsync("key5")));
            Assert.NotNull((await _subject.GetAsync("key6")));
            Assert.NotNull((await _subject.GetAsync("key7")));
        }
        {
            await Populate();
            await _subject.RemoveAllAsync(new PersistedGrantFilter
            {
                SubjectId = "sub2",
                ClientId = "client4",
                SessionId = "session5"
            });
            Assert.NotNull((await _subject.GetAsync("key1")));
            Assert.NotNull((await _subject.GetAsync("key2")));
            Assert.NotNull((await _subject.GetAsync("key3")));
            Assert.NotNull((await _subject.GetAsync("key4")));
            Assert.NotNull((await _subject.GetAsync("key5")));
            Assert.NotNull((await _subject.GetAsync("key6")));
            Assert.NotNull((await _subject.GetAsync("key7")));
        }
        {
            await Populate();
            await _subject.RemoveAllAsync(new PersistedGrantFilter
            {
                SubjectId = "sub3",
                ClientId = "client1",
                SessionId = "session1"
            });
            Assert.NotNull((await _subject.GetAsync("key1")));
            Assert.NotNull((await _subject.GetAsync("key2")));
            Assert.NotNull((await _subject.GetAsync("key3")));
            Assert.NotNull((await _subject.GetAsync("key4")));
            Assert.NotNull((await _subject.GetAsync("key5")));
            Assert.NotNull((await _subject.GetAsync("key6")));
            Assert.NotNull((await _subject.GetAsync("key7")));
        }
    }

    private async Task Populate()
    {
        await _subject.StoreAsync(new PersistedGrant() { Key = "key1", SubjectId = "sub1", ClientId = "client1", SessionId = "session1" });
        await _subject.StoreAsync(new PersistedGrant() { Key = "key2", SubjectId = "sub1", ClientId = "client2", SessionId = "session1" });
        await _subject.StoreAsync(new PersistedGrant() { Key = "key3", SubjectId = "sub1", ClientId = "client1", SessionId = "session2" });
        await _subject.StoreAsync(new PersistedGrant() { Key = "key4", SubjectId = "sub1", ClientId = "client3", SessionId = "session2" });
        await _subject.StoreAsync(new PersistedGrant() { Key = "key5", SubjectId = "sub1", ClientId = "client4", SessionId = "session3" });
        await _subject.StoreAsync(new PersistedGrant() { Key = "key6", SubjectId = "sub1", ClientId = "client4", SessionId = "session4" });

        await _subject.StoreAsync(new PersistedGrant() { Key = "key7", SubjectId = "sub2", ClientId = "client4", SessionId = "session4" });
    }
}
