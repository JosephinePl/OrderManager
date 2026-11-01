namespace OrderManager.Data.Tests.Repositories;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using OrderManager.Core.Entities;
using OrderManager.Data.Repositories;

/// <summary>
/// Integration tests for <see cref="RepositoryBase{T}"/> using an EF Core in-memory database.
/// </summary>
[TestClass]
public sealed class RepositoryBaseTests : IAsyncDisposable
{
    #region Fields

    /// <summary>
    /// The in-memory database context under test.
    /// </summary>
    private OrderManagerDbContext _context = null!;

    /// <summary>
    /// A substituted logger passed to the repository.
    /// </summary>
    private ILogger<RepositoryBase<Order>> _logger = null!;

    /// <summary>
    /// The system under test — a concrete subtype of <see cref="RepositoryBase{T}"/>.
    /// </summary>
    private OrderRepository _sut = null!;

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets the MSTest context for the current test.
    /// </summary>
    public TestContext TestContext { get; set; } = null!;

    #endregion

    #region Test Lifecycle

    /// <summary>
    /// Creates a fresh in-memory database and repository instance before each test.
    /// </summary>
    [TestInitialize]
    public void Initialize()
    {
        DbContextOptions<OrderManagerDbContext> options = new DbContextOptionsBuilder<OrderManagerDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new OrderManagerDbContext(options);
        _logger = Substitute.For<ILogger<RepositoryBase<Order>>>();
        _sut = new OrderRepository(_context, _logger);
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

    #region GetByIdAsync

    /// <summary>
    /// Returns the entity when it exists in the database.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [TestMethod]
    public async Task WhenGetByIdAsyncAndEntityExists_ThenEntityIsReturned()
    {
        // Arrange.
        Order order = CreateOrder();
        await _context.Orders.AddAsync(order, TestContext.CancellationToken);
        await _context.SaveChangesAsync(TestContext.CancellationToken);

        // Act.
        Order? result = await _sut.GetByIdAsync(order.Id, TestContext.CancellationToken);

        // Assert.
        Assert.AreEqual(order, result);
    }

    /// <summary>
    /// Returns <see langword="null"/> when no entity with the given ID exists.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [TestMethod]
    public async Task WhenGetByIdAsyncAndEntityDoesNotExist_ThenNullIsReturned()
    {
        // Act.
        Order? result = await _sut.GetByIdAsync(Guid.NewGuid(), TestContext.CancellationToken);

        // Assert.
        Assert.IsNull(result);
    }

    #endregion

    #region GetAllAsync

    /// <summary>
    /// Returns all entities when the store is non-empty.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [TestMethod]
    public async Task WhenGetAllAsyncAndEntitiesExist_ThenAllEntitiesAreReturned()
    {
        // Arrange.
        await _context.Orders.AddRangeAsync(new[] { CreateOrder(), CreateOrder() }, TestContext.CancellationToken);
        await _context.SaveChangesAsync(TestContext.CancellationToken);

        // Act.
        IReadOnlyList<Order> result = await _sut.GetAllAsync(TestContext.CancellationToken);

        // Assert.
        Assert.HasCount(2, result);
    }

    /// <summary>
    /// Returns an empty list when the store contains no entities.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [TestMethod]
    public async Task WhenGetAllAsyncAndNoEntitiesExist_ThenEmptyListIsReturned()
    {
        // Act.
        IReadOnlyList<Order> result = await _sut.GetAllAsync(TestContext.CancellationToken);

        // Assert.
        Assert.IsEmpty(result);
    }

    #endregion

    #region AddAsync

    /// <summary>
    /// Persists the entity so it can subsequently be retrieved by ID.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [TestMethod]
    public async Task WhenAddAsyncAndEntityIsValid_ThenEntityIsPersisted()
    {
        // Arrange.
        Order order = CreateOrder();

        // Act.
        await _sut.AddAsync(order, TestContext.CancellationToken);

        // Assert.
        Order? result = await _context.Orders.FindAsync(new object[] { order.Id }, TestContext.CancellationToken);
        Assert.IsNotNull(result);
    }

    /// <summary>
    /// Throws <see cref="ArgumentNullException"/> when the entity is null.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [TestMethod]
    public async Task WhenAddAsyncAndEntityIsNull_ThenArgumentNullExceptionIsThrown()
    {
        // Act & Assert.
        await Assert.ThrowsExactlyAsync<ArgumentNullException>(() => _sut.AddAsync(null!, TestContext.CancellationToken));
    }

    #endregion

    #region UpdateAsync

    /// <summary>
    /// Persists property changes on an existing entity.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [TestMethod]
    public async Task WhenUpdateAsyncAndEntityIsValid_ThenChangesArePersisted()
    {
        // Arrange.
        Order order = CreateOrder();
        await _context.Orders.AddAsync(order, TestContext.CancellationToken);
        await _context.SaveChangesAsync(TestContext.CancellationToken);
        _context.ChangeTracker.Clear();
        Order updated = order with { Name = "Updated Name" };

        // Act.
        await _sut.UpdateAsync(updated, TestContext.CancellationToken);

        // Assert.
        Order? result = await _context.Orders.FindAsync(new object[] { order.Id }, TestContext.CancellationToken);
        Assert.AreEqual("Updated Name", result?.Name);
    }

    /// <summary>
    /// Throws <see cref="ArgumentNullException"/> when the entity is null.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [TestMethod]
    public async Task WhenUpdateAsyncAndEntityIsNull_ThenArgumentNullExceptionIsThrown()
    {
        // Act & Assert.
        await Assert.ThrowsExactlyAsync<ArgumentNullException>(() => _sut.UpdateAsync(null!, TestContext.CancellationToken));
    }

    #endregion

    #region DeleteAsync

    /// <summary>
    /// Removes the entity from the store when it exists.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [TestMethod]
    public async Task WhenDeleteAsyncAndEntityExists_ThenEntityIsRemoved()
    {
        // Arrange.
        Order order = CreateOrder();
        await _context.Orders.AddAsync(order, TestContext.CancellationToken);
        await _context.SaveChangesAsync(TestContext.CancellationToken);
        _context.ChangeTracker.Clear();

        // Act.
        await _sut.DeleteAsync(order.Id, TestContext.CancellationToken);

        // Assert.
        Order? result = await _context.Orders.FindAsync(new object[] { order.Id }, TestContext.CancellationToken);
        Assert.IsNull(result);
    }

    /// <summary>
    /// Logs a warning and does not throw when no entity with the given ID exists.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [TestMethod]
    public async Task WhenDeleteAsyncAndEntityDoesNotExist_ThenWarningIsLogged()
    {
        // Act.
        await _sut.DeleteAsync(Guid.NewGuid(), TestContext.CancellationToken);

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