using LinkUpPro.Core.Application;
using LinkUpPro.Core.Application.Dtos.Social;
using LinkUpPro.Core.Application.Interfaces;
using LinkUpPro.Core.Domain.Exceptions;
using LinkUpPro.Core.Domain.Entities;
using LinkUpPro.Core.Domain.Enums;
using LinkUpPro.Infrastructure.Persistence;
using LinkUpPro.Infrastructure.Persistence.Contexts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LinkUpPro.Tests;

/// <summary>Supplies trusted test identities and a controllable clock without changing production authentication.</summary>
internal sealed class TestUserDirectory : IUserDirectoryService
{
    public List<UserSummaryDto> Users { get; } = [];
    public Task<UserSummaryDto?> GetByIdAsync(string id)
        => Task.FromResult(Users.SingleOrDefault(u => string.Equals(u.Id, id, StringComparison.OrdinalIgnoreCase)));
    public Task<List<UserSummaryDto>> GetByIdsAsync(IEnumerable<string> ids)
        => Task.FromResult(Users.Where(u => ids.Contains(u.Id)).ToList());
    public Task<List<UserSummaryDto>> SearchActiveAsync(string? search)
        => Task.FromResult(Users.Where(u => u.IsActive && (string.IsNullOrWhiteSpace(search)
            || u.UserName.Contains(search.Trim(), StringComparison.OrdinalIgnoreCase))).ToList());
}

/// <summary>Represents one authenticated test actor; it is never registered by production Web.</summary>
internal sealed class TestCurrentUser(string id) : ICurrentUserService
{
    public string UserId { get; set; } = id;
    public string GetRequiredUserId() => UserId;
}

/// <summary>Advances time deterministically for abandonment and duration scenarios.</summary>
internal sealed class TestClock : TimeProvider
{
    public DateTimeOffset Now { get; set; } = new(2026, 10, 19, 0, 0, 0, TimeSpan.Zero);
    public override DateTimeOffset GetUtcNow() => Now;
}

/// <summary>Builds real application services and SQL repositories around unique users for each test.</summary>
internal sealed class TestEnvironment : IAsyncDisposable
{
    private readonly ServiceProvider provider;
    private readonly IServiceScope scope;
    public TestCurrentUser Current { get; }
    public TestUserDirectory Directory { get; }
    public TestClock Clock { get; }
    public string Alice => Directory.Users[0].Id;
    public string Bob => Directory.Users[1].Id;
    public string Charlie => Directory.Users[2].Id;
    public string Inactive => Directory.Users[3].Id;
    public LinkUpProAppContext Context => Get<LinkUpProAppContext>();

    public TestEnvironment(SqlDatabaseFixture database, TestUserDirectory? directory = null, string? actor = null, TestClock? clock = null)
    {
        var prefix = Guid.NewGuid().ToString("N");
        Directory = directory ?? new TestUserDirectory();
        if (directory is null)
            foreach (var (name, suffix) in new[] { ("Alice", "a"), ("Bob", "b"), ("Charlie", "c"), ("Inactive", "d") })
                Directory.Users.Add(new() { Id = prefix + suffix, UserName = name, FirstName = name,
                    LastName = "Tester", IsActive = suffix != "d" });
        Current = new(actor ?? Alice);
        Clock = clock ?? new();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplicationLayerIoc();
        services.AddPersistenceLayerIoc(new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?> { ["ConnectionStrings:DefaultConnection"] = database.ConnectionString }).Build());
        services.AddSingleton<ICurrentUserService>(Current);
        services.AddSingleton<IUserDirectoryService>(Directory);
        services.AddSingleton<IUserAccountStatusService>(new TestAccountStatus(Directory));
        services.AddSingleton<TimeProvider>(Clock);
        provider = services.BuildServiceProvider();
        scope = provider.CreateScope();
    }

    /// <summary>Resolves the same registered service implementation used by Web.</summary>
    public T Get<T>() where T : notnull => scope.ServiceProvider.GetRequiredService<T>();

    /// <summary>Seeds a valid relationship without exercising a different module's account registration.</summary>
    public async Task MakeFriendsAsync(string first, string second)
    {
        var pair = LinkUpPro.Core.Application.Services.FriendshipService.OrderPair(first, second);
        Context.Friendships.Add(new() { User1Id = pair.First, User2Id = pair.Second, Status = FriendshipStatus.Active });
        await Context.SaveChangesAsync();
    }

    /// <summary>Seeds one visible image publication for integration checks.</summary>
    public async Task<Post> AddPostAsync(string author)
    {
        var post = new Post { Id = 0, UserId = author, Content = "Example publication", Privacy = PostPrivacy.FriendsOnly,
            MediaType = PostMediaType.Image, ImagePath = "/images/example.png", AllowComments = true,
            CreatedAt = Clock.GetUtcNow().UtcDateTime };
        Context.Posts.Add(post);
        await Context.SaveChangesAsync();
        return post;
    }

    public async ValueTask DisposeAsync()
    {
        scope.Dispose();
        await provider.DisposeAsync();
    }
}
