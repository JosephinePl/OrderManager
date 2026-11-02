namespace OrderManager.Data.Tests.Adapters;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using OrderManager.Core.Entities;
using OrderManager.Data.Adapters;

/// <summary>
/// Integration tests for <see cref="EfCoreDataAdapter"/> using an EF Core in-memory database.
/// </summary>
[TestClass]
public sealed class EfCoreDataAdapterTests : IAsyncDisposable
{
    #region Fields

    /// <summary>
    /// The in-memory database context under test.
    /// </summary>
    private OrderManagerDbContext _context = null!;

    /// <summary>
    /// A substituted logger passed to the adapter.
    /// </summary>
    private ILogger<EfCoreDataAdapter> _logger = null!;

    /// <summary>
    /// The system under test.
    /// </summary>
    private EfCoreDataAdapter _sut = null!;

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets the MSTest context for the current test.
    /// </summary>
    public TestContext TestContext { get; set; } = null!;

    #endregion

    #region Test Lifecycle

    /// <summary>
    /// Creates a fresh in-memory database and adapter instance before each test.
    /// </summary>
    [TestInitialize]
    public void Initialize()
    {
        DbContextOptions<OrderManagerDbContext> options = new DbContextOptionsBuilder<OrderManagerDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new OrderManagerDbContext(options);
        _logger = Substitute.For<ILogger<EfCoreDataAdapter>>();
        _sut = new EfCoreDataAdapter(_context, _logger);
    }

    /// <summary>
    /// Disposes the database context after each test.
    /// </summary>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous dispose operation.</returns>
    public async ValueTask DisposeAsync()
    {
        await _context.DisposeAsync();
    }

    #endregion

    #region GetOrderByIdAsync

    /// <summary>
    /// Returns the entity when it exists in the database.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [TestMethod]
    public async Task WhenGetOrderByIdAsyncAndEntityExists_ThenEntityIsReturned()
    {
        // Arrange.
        Order order = CreateOrder();
        await _context.Orders.AddAsync(order, TestContext.CancellationToken);
        await _context.SaveChangesAsync(TestContext.CancellationToken);

        // Act.
        Order? result = await _sut.GetOrderByIdAsync(order.Id, TestContext.CancellationToken);

        // Assert.
        Assert.AreEqual(order, result);
    }

    /// <summary>
    /// Returns <see langword="null"/> when no entity with the given ID exists.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [TestMethod]
    public async Task WhenGetOrderByIdAsyncAndEntityDoesNotExist_ThenNullIsReturned()
    {
        // Act.
        Order? result = await _sut.GetOrderByIdAsync(Guid.NewGuid(), TestContext.CancellationToken);

        // Assert.
        Assert.IsNull(result);
    }

    #endregion

    #region GetAllOrdersAsync

    /// <summary>
    /// Returns all entities when the store is non-empty.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [TestMethod]
    public async Task WhenGetAllOrdersAsyncAndEntitiesExist_ThenAllEntitiesAreReturned()
    {
        // Arrange.
        await _context.Orders.AddRangeAsync(new[] { CreateOrder(), CreateOrder() }, TestContext.CancellationToken);
        await _context.SaveChangesAsync(TestContext.CancellationToken);

        // Act.
        IReadOnlyList<Order> result = await _sut.GetAllOrdersAsync(TestContext.CancellationToken);

        // Assert.
        Assert.HasCount(2, result);
    }

    /// <summary>
    /// Returns an empty list when the store contains no entities.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [TestMethod]
    public async Task WhenGetAllOrdersAsyncAndNoEntitiesExist_ThenEmptyListIsReturned()
    {
        // Act.
        IReadOnlyList<Order> result = await _sut.GetAllOrdersAsync(TestContext.CancellationToken);

        // Assert.
        Assert.IsEmpty(result);
    }

    #endregion

    #region AddOrderAsync

    /// <summary>
    /// Persists the entity so it can subsequently be retrieved by ID.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [TestMethod]
    public async Task WhenAddOrderAsyncAndEntityIsValid_ThenEntityIsPersisted()
    {
        // Arrange.
        Order order = CreateOrder();

        // Act.
        await _sut.AddOrderAsync(order, TestContext.CancellationToken);

        // Assert.
        Order? result = await _context.Orders.FindAsync(new object[] { order.Id }, TestContext.CancellationToken);
        Assert.IsNotNull(result);
    }

    /// <summary>
    /// Throws <see cref="ArgumentNullException"/> when the entity is null.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [TestMethod]
    public async Task WhenAddOrderAsyncAndEntityIsNull_ThenArgumentNullExceptionIsThrown()
    {
        // Act & Assert.
        await Assert.ThrowsExactlyAsync<ArgumentNullException>(() => _sut.AddOrderAsync(null!, TestContext.CancellationToken));
    }

    #endregion

    #region UpdateOrderAsync

    /// <summary>
    /// Persists property changes on an existing entity.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [TestMethod]
    public async Task WhenUpdateOrderAsyncAndEntityIsValid_ThenChangesArePersisted()
    {
        // Arrange.
        Order order = CreateOrder();
        await _context.Orders.AddAsync(order, TestContext.CancellationToken);
        await _context.SaveChangesAsync(TestContext.CancellationToken);
        _context.ChangeTracker.Clear();
        Order updated = order with { Name = "Updated Name" };

        // Act.
        await _sut.UpdateOrderAsync(updated, TestContext.CancellationToken);

        // Assert.
        Order? result = await _context.Orders.FindAsync(new object[] { order.Id }, TestContext.CancellationToken);
        Assert.AreEqual("Updated Name", result?.Name);
    }

    /// <summary>
    /// Throws <see cref="ArgumentNullException"/> when the entity is null.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [TestMethod]
    public async Task WhenUpdateOrderAsyncAndEntityIsNull_ThenArgumentNullExceptionIsThrown()
    {
        // Act & Assert.
        await Assert.ThrowsExactlyAsync<ArgumentNullException>(() => _sut.UpdateOrderAsync(null!, TestContext.CancellationToken));
    }

    #endregion

    #region DeleteOrderAsync

    /// <summary>
    /// Removes the entity from the store when it exists.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [TestMethod]
    public async Task WhenDeleteOrderAsyncAndEntityExists_ThenEntityIsRemoved()
    {
        // Arrange.
        Order order = CreateOrder();
        await _context.Orders.AddAsync(order, TestContext.CancellationToken);
        await _context.SaveChangesAsync(TestContext.CancellationToken);
        _context.ChangeTracker.Clear();

        // Act.
        await _sut.DeleteOrderAsync(order.Id, TestContext.CancellationToken);

        // Assert.
        Order? result = await _context.Orders.FindAsync(new object[] { order.Id }, TestContext.CancellationToken);
        Assert.IsNull(result);
    }

    /// <summary>
    /// Logs a warning and does not throw when no entity with the given ID exists.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [TestMethod]
    public async Task WhenDeleteOrderAsyncAndEntityDoesNotExist_ThenWarningIsLogged()
    {
        // Act.
        await _sut.DeleteOrderAsync(Guid.NewGuid(), TestContext.CancellationToken);

        // Assert.
        _logger.ReceivedWithAnyArgs(1).Log<object>(LogLevel.Warning, default, default!, null, default!);
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Creates an <see cref="Order"/> with an optional ID override.
    /// </summary>
    /// <param name="id">The ID to assign; a new <see cref="Guid"/> is used when <see langword="null"/>.</param>
    /// <returns>A new <see cref="Order"/> instance.</returns>
    private static Order CreateOrder(Guid? id = null)
    {
        return new Order(id ?? Guid.NewGuid(), "Order", "Description", DateTimeOffset.UtcNow);
    }

    #endregion
}