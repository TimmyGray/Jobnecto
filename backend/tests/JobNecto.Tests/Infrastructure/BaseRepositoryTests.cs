using FluentAssertions;
using Microsoft.EntityFrameworkCore;

public class TestEntity : BaseEntity { }

public class TestDbContext : DbContext
{
    public TestDbContext(DbContextOptions<TestDbContext> options)
        : base(options) { }

    public DbSet<TestEntity> TestEntities { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TestEntity>(b =>
        {
            b.HasKey(e => e.Id);
            b.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            b.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");
            b.Property(e => e.CreatedAt).ValueGeneratedOnAdd();
            b.Property(e => e.UpdatedAt).ValueGeneratedOnAddOrUpdate();
        });
    }
}

public class TestRepository : BaseRepository<TestEntity>
{
    public TestRepository(DbContext context)
        : base(context) { }
}

public class TestData
{
    public static List<TestEntity> Entities = new List<TestEntity>
    {
        new TestEntity
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        },
        new TestEntity
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000002"),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        },
        new TestEntity
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000003"),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        },
        new TestEntity
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000004"),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        },
        new TestEntity
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000005"),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        },
        new TestEntity
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000006"),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        },
        new TestEntity
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000007"),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        },
        new TestEntity
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000008"),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        },
        new TestEntity
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000009"),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        },
        new TestEntity
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000010"),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        },
        new TestEntity
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000011"),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        },
        new TestEntity
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000012"),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        },
        new TestEntity
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000013"),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        },
        new TestEntity
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000014"),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        },
        new TestEntity
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000015"),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        },
        new TestEntity
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000016"),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        },
        new TestEntity
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000017"),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        },
        new TestEntity
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000018"),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        },
        new TestEntity
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000019"),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        },
        new TestEntity
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000020"),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        },
        new TestEntity
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000021"),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        },
    };
}

public class BaseRepositoryTests
{
    private static IReadOnlyList<TestEntity> OrderedEntities =>
        TestData.Entities.OrderByDescending(e => e.UpdatedAt).ThenByDescending(e => e.Id).ToList();

    public static IEnumerable<object[]> TestPagedQueries
    {
        get
        {
            return new List<object[]>
            {
                new object[]
                {
                    new PagedQuery { },
                    new PagedResult<TestEntity>(
                        OrderedEntities.Take(20).ToList(),
                        TestData.Entities.Count,
                        OrderedEntities.Take(20).Last().Id,
                        OrderedEntities.Take(20).Last().UpdatedAt,
                        20,
                        true
                    ),
                },
                new object[]
                {
                    new PagedQuery { PageSize = 10 },
                    new PagedResult<TestEntity>(
                        OrderedEntities.Take(10).ToList(),
                        TestData.Entities.Count,
                        OrderedEntities[9].Id,
                        OrderedEntities[9].UpdatedAt,
                        10,
                        true
                    ),
                },
                new object[]
                {
                    new PagedQuery
                    {
                        PageSize = 10,
                        LastSeenId = OrderedEntities[10].Id,
                        LastSeenUpdatedAt = OrderedEntities[10].UpdatedAt,
                    },
                    new PagedResult<TestEntity>(
                        OrderedEntities.Skip(11).Take(10).ToList(),
                        TestData.Entities.Count,
                        OrderedEntities.Skip(11).Take(10).Last().Id,
                        OrderedEntities.Skip(11).Take(10).Last().UpdatedAt,
                        10,
                        false
                    ),
                },
                new object[]
                {
                    new PagedQuery
                    {
                        PageSize = 5,
                        LastSeenId = OrderedEntities[10].Id,
                        LastSeenUpdatedAt = OrderedEntities[10].UpdatedAt,
                    },
                    new PagedResult<TestEntity>(
                        OrderedEntities.Skip(11).Take(5).ToList(),
                        TestData.Entities.Count,
                        OrderedEntities.Skip(11).Take(5).Last().Id,
                        OrderedEntities.Skip(11).Take(5).Last().UpdatedAt,
                        5,
                        true
                    ),
                },
                new object[]
                {
                    new PagedQuery
                    {
                        PageSize = 20,
                        LastSeenId = OrderedEntities[11].Id,
                        LastSeenUpdatedAt = OrderedEntities[11].UpdatedAt,
                    },
                    new PagedResult<TestEntity>(
                        OrderedEntities.Skip(12).Take(20).ToList(),
                        TestData.Entities.Count,
                        OrderedEntities.Skip(12).Take(20).Last().Id,
                        OrderedEntities.Skip(12).Take(20).Last().UpdatedAt,
                        20,
                        false
                    ),
                },
                new object[]
                {
                    new PagedQuery
                    {
                        PageSize = 30,
                        LastSeenId = OrderedEntities[20].Id,
                        LastSeenUpdatedAt = OrderedEntities[20].UpdatedAt,
                    },
                    new PagedResult<TestEntity>(
                        new List<TestEntity>(),
                        TestData.Entities.Count,
                        null,
                        null,
                        30,
                        false
                    ),
                },
            };
        }
    }

    private TestDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        return new TestDbContext(options);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsEntity_WhenEntityExists()
    {
        var context = CreateContext("GetByIdAsyncExists");
        var repository = new TestRepository(context);

        context.TestEntities.AddRange(TestData.Entities);
        context.SaveChanges();

        var result = await repository.GetByIdAsync(TestData.Entities[0].Id, CancellationToken.None);

        result.Should().NotBeNull().And.BeEquivalentTo(TestData.Entities[0]);
    }

    [Fact]
    public async Task GetByIdAsync_ThrowsException_WhenEntityDoesNotExist()
    {
        var context = CreateContext("GetByIdAsyncDoesNotExist");
        var repository = new TestRepository(context);

        context.TestEntities.AddRange(TestData.Entities);
        context.SaveChanges();
        var randomId = Guid.NewGuid();

        Func<Task> act = async () =>
            await repository.GetByIdAsync(randomId, CancellationToken.None);

        await act.Should()
            .ThrowAsync<Exception>()
            .WithMessage($"Entity with id {randomId} not found");
    }

    [Fact]
    public async Task GetAsync_ReturnsEntities_WhenEntitiesExist()
    {
        var context = CreateContext("GetAsyncExists");
        var repository = new TestRepository(context);

        context.TestEntities.AddRange(TestData.Entities);
        context.SaveChanges();

        var pagedQuery = new PagedQuery { PageSize = TestData.Entities.Count };
        var result = await repository.GetAsync(pagedQuery, CancellationToken.None);
        result
            .Items.Should()
            .HaveCount(TestData.Entities.Count)
            .And.BeEquivalentTo(OrderedEntities)
            .And.Subject.Should()
            .BeInDescendingOrder(e => e.UpdatedAt)
            .And.Subject.Should()
            .BeInDescendingOrder(e => e.Id);
    }

    [Fact]
    public async Task GetAsync_ReturnsEmptyList_WhenNoEntitiesExist()
    {
        var context = CreateContext("GetAsyncDoesNotExist");
        var repository = new TestRepository(context);

        var result = await repository.GetAsync(new PagedQuery(), CancellationToken.None);
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        result.PageSize.Should().Be(20);
        result.HasNext.Should().BeFalse();
        result.LastSeenId.Should().BeNull();
        result.LastSeenUpdatedAt.Should().BeNull();
    }

    [Theory]
    [MemberData(nameof(TestPagedQueries))]
    public async Task GetAsync_ReturnsCorrectPagedResult_WhenEntitiesExistAndPagedQueryIsProvided(
        PagedQuery pagedQuery,
        PagedResult<TestEntity> expectedResult
    )
    {
        var context = CreateContext($"GetAsyncExistsPagedResult_{Guid.NewGuid()}");
        var repository = new TestRepository(context);

        context.TestEntities.AddRange(TestData.Entities);
        context.SaveChanges();

        var result = await repository.GetAsync(pagedQuery, CancellationToken.None);
        result.Should().BeEquivalentTo(expectedResult);
        result.Items.Should().HaveCount(expectedResult.Items.Count);
        result.Items.Should().BeEquivalentTo(expectedResult.Items);
        result.TotalCount.Should().Be(expectedResult.TotalCount);
        result.PageSize.Should().Be(expectedResult.PageSize);
        result.TotalPages.Should().Be(expectedResult.TotalPages);
        result.HasNext.Should().Be(expectedResult.HasNext);
        result.LastSeenId.Should().Be(expectedResult.LastSeenId);
        result.LastSeenUpdatedAt.Should().Be(expectedResult.LastSeenUpdatedAt);
    }

    [Fact]
    public async Task GetAsync_ReturnsDefaultPagedResult_WhenPagedQueryIsInvalid()
    {
        var context = CreateContext("GetAsyncInvalidPagedQuery");
        var repository = new TestRepository(context);
        var invalidPagedQuery = new PagedQuery
        {
            LastSeenId = Guid.NewGuid(),
            LastSeenUpdatedAt = DateTime.Parse("2026-03-23 12:00:00"),
        };

        context.TestEntities.AddRange(TestData.Entities);
        context.SaveChanges();

        var result = await repository.GetAsync(invalidPagedQuery, CancellationToken.None);
        result
            .Should()
            .BeEquivalentTo(
                new PagedResult<TestEntity>(
                    OrderedEntities.Take(20).ToList(),
                    OrderedEntities.Count,
                    OrderedEntities.Take(20).Last().Id,
                    OrderedEntities.Take(20).Last().UpdatedAt,
                    20,
                    true
                )
            );

        result
            .Items.Should()
            .BeInDescendingOrder(e => e.UpdatedAt)
            .And.BeInDescendingOrder(e => e.Id);

        result.TotalPages.Should().Be(2);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(int.MinValue)]
    public async Task GetAsync_ThrowsException_WhenPageSizeIsInvalid(int pageSize)
    {
        var context = CreateContext($"GetAsyncInvalidPageSize_{Guid.NewGuid()}");
        var repository = new TestRepository(context);

        context.TestEntities.AddRange(TestData.Entities);
        context.SaveChanges();

        var pagedQuery = new PagedQuery { PageSize = pageSize };

        Func<Task> act = async () => await repository.GetAsync(pagedQuery, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task DeleteAsync_DeletesEntity_WhenEntityExists()
    {
        var context = CreateContext("DeleteAsyncExists");
        var repository = new TestRepository(context);

        context.TestEntities.AddRange(TestData.Entities);
        context.SaveChanges();

        var result = await repository.DeleteAsync(TestData.Entities[0].Id, CancellationToken.None);
        context.SaveChanges();
        result.Should().Be(TestData.Entities[0].Id);
        context.TestEntities.Should().NotContain(e => e.Id == TestData.Entities[0].Id);
        context.TestEntities.Should().HaveCount(TestData.Entities.Count - 1);
    }

    [Fact]
    public async Task DeleteAsync_ThrowsException_WhenEntityDoesNotExist()
    {
        var context = CreateContext("DeleteAsyncDoesNotExist");
        var repository = new TestRepository(context);
        var randomId = Guid.NewGuid();

        Func<Task> act = async () => await repository.DeleteAsync(randomId, CancellationToken.None);
        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task CreateAsync_CreatesEntity_WhenEntityIsProvided()
    {
        var context = CreateContext("CreateAsyncExists");
        var repository = new TestRepository(context);
        var entity = new TestEntity
        {
            Id = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        var result = await repository.CreateAsync(entity, CancellationToken.None);
        context.SaveChanges();
        result.Should().Be(entity);
        context.TestEntities.Should().Contain(e => e.Id == entity.Id);
        context.TestEntities.Should().HaveCount(1);
    }
}
