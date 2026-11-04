namespace OrderManager.Data.Tests.Integration;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using OrderManager.Core.Entities;
using OrderManager.Data.Adapters;
using Testcontainers.PostgreSql;

/// <summary>
/// Integration tests for <see cref="EfCoreDataAdapter"/> against a real PostgreSQL instance.
/// These cover paths the in-memory provider cannot exercise: real FK constraints and eager-load joins.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class EfCoreDataAdapterIntegrationTests : IAsyncDisposable
{
    /// <summary>Shared Postgres container for all tests in this class.</summary>
    private static PostgreSqlContainer _postgres = null!;

    /// <summary>EF Core options pointing at the shared container.</summary>
    private static DbContextOptions<OrderManagerDbContext> _options = null!;

    /// <summary>Per-test database context.</summary>
    private OrderManagerDbContext _context = null!;

    /// <summary>System under test.</summary>
    private EfCoreDataAdapter _testObject = null!;

    /// <summary>Gets or sets the MSTest context for the current test.</summary>
    public TestContext TestContext { get; set; } = null!;

    /// <summary>Starts a single Postgres container and applies migrations once for all tests.</summary>
    /// <param name="context">The MSTest context provided by the test runner.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [ClassInitialize]
    public static async Task ClassInitialize(TestContext context)
    {
        _postgres = new PostgreSqlBuilder().Build();
        await _postgres.StartAsync(CancellationToken.None);

        _options = new DbContextOptionsBuilder<OrderManagerDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        await using OrderManagerDbContext ctx = new(_options);
        await ctx.Database.MigrateAsync(CancellationToken.None);
    }

    /// <summary>Stops and disposes the shared Postgres container.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [ClassCleanup]
    public static async Task ClassCleanup()
    {
        await _postgres.DisposeAsync();
    }

    /// <summary>Creates a fresh context and adapter before each test.</summary>
    [TestInitialize]
    public void Initialize()
    {
        (_context, _testObject) = CreateContextAndTestObject();
    }

    /// <summary>Truncates all data and disposes the context after each test.</summary>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
    public async ValueTask DisposeAsync()
    {
        await _context.Database.ExecuteSqlRawAsync(
            """TRUNCATE "OrderBoards", "BoardComponents", "Orders", "Boards", "Components" CASCADE""",
            CancellationToken.None);
        await _context.DisposeAsync();
    }

    /// <summary>
    /// <see cref="EfCoreDataAdapter.GetOrderWithDetailsAsync"/> must return a fully-populated graph
    /// (order → boards → components) using real Postgres joins.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [TestMethod]
    public async Task WhenGetOrderWithDetailsAsyncAndOrderExists_ThenFullGraphIsReturned()
    {
        Order order = await SeedOrderAsync();
        Board board = await SeedBoardAsync();
        Component component = await SeedComponentAsync();

        await _testObject.AddBoardToOrderAsync(order.Id, board.Id, 1, TestContext.CancellationToken);
        await _testObject.AddComponentToBoardAsync(board.Id, component.Id, 3, TestContext.CancellationToken);

        // Fresh context — no change-tracker cache.
        (OrderManagerDbContext verifyCtx, EfCoreDataAdapter verifyAdapter) = CreateContextAndTestObject();
        await using (verifyCtx)
        {
            Order? result = await verifyAdapter.GetOrderWithDetailsAsync(order.Id, TestContext.CancellationToken);

            Assert.IsNotNull(result);
            Assert.HasCount(1, result.OrderBoards);
            OrderBoard orderBoard = result.OrderBoards[0];
            Assert.AreEqual(board.Id, orderBoard.Board.Id);
            Assert.HasCount(1, orderBoard.Board.BoardComponents);
            BoardComponent boardComponent = orderBoard.Board.BoardComponents[0];
            Assert.AreEqual(component.Id, boardComponent.Component.Id);
            Assert.AreEqual(3, boardComponent.QuantityPerBoard);
        }
    }

    /// <summary>
    /// Concurrent calls to <see cref="EfCoreDataAdapter.AddBoardToOrderAsync"/> for the same pair must not
    /// throw and must leave exactly one <c>OrderBoards</c> row (last-write-wins via <c>ON CONFLICT DO UPDATE</c>).
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [TestMethod]
    public async Task WhenAddBoardToOrderAsyncAndConcurrentUpsertsOccur_ThenExactlyOneRowExists()
    {
        Order order = await SeedOrderAsync();
        Board board = await SeedBoardAsync();

        // Ten concurrent writers, each claiming a different quantity.
        Task[] writers = Enumerable.Range(1, 10).Select(i =>
        {
            (OrderManagerDbContext ctx, EfCoreDataAdapter adapter) = CreateContextAndTestObject();
            return adapter.AddBoardToOrderAsync(order.Id, board.Id, i, TestContext.CancellationToken)
                .ContinueWith(_ => ctx.DisposeAsync().AsTask(), TaskScheduler.Default).Unwrap();
        }).ToArray();

        await Task.WhenAll(writers);

        int count = await _context.Set<OrderBoard>()
            .CountAsync(ob => ob.OrderId == order.Id && ob.BoardId == board.Id, TestContext.CancellationToken);

        Assert.AreEqual(1, count);
    }

    /// <summary>Creates a new <see cref="OrderManagerDbContext"/> and <see cref="EfCoreDataAdapter"/> pair.</summary>
    /// <returns>A tuple of the context and adapter.</returns>
    private static (OrderManagerDbContext Context, EfCoreDataAdapter Adapter) CreateContextAndTestObject()
    {
        OrderManagerDbContext databaseContext = new(_options);
        ILogger<EfCoreDataAdapter> logger = Substitute.For<ILogger<EfCoreDataAdapter>>();
        return (databaseContext, new EfCoreDataAdapter(databaseContext, logger));
    }

    /// <summary>Inserts a seed <see cref="Order"/> and returns it.</summary>
    /// <returns>The persisted <see cref="Order"/>.</returns>
    private async Task<Order> SeedOrderAsync()
    {
        Order order = new(Guid.NewGuid(), "Order", "Description", DateTimeOffset.UtcNow);
        await _context.Orders.AddAsync(order, TestContext.CancellationToken);
        await _context.SaveChangesAsync(TestContext.CancellationToken);
        _context.ChangeTracker.Clear();
        return order;
    }

    /// <summary>Inserts a seed <see cref="Board"/> and returns it.</summary>
    /// <returns>The persisted <see cref="Board"/>.</returns>
    private async Task<Board> SeedBoardAsync()
    {
        Board board = new(Guid.NewGuid(), "Board", "Description", 100m, 50m);
        await _context.Boards.AddAsync(board, TestContext.CancellationToken);
        await _context.SaveChangesAsync(TestContext.CancellationToken);
        _context.ChangeTracker.Clear();
        return board;
    }

    /// <summary>Inserts a seed <see cref="Component"/> and returns it.</summary>
    /// <returns>The persisted <see cref="Component"/>.</returns>
    private async Task<Component> SeedComponentAsync()
    {
        Component component = new(Guid.NewGuid(), "Component", "Description", 100);
        await _context.Components.AddAsync(component, TestContext.CancellationToken);
        await _context.SaveChangesAsync(TestContext.CancellationToken);
        _context.ChangeTracker.Clear();
        return component;
    }
}