namespace OrderManager.Business.Tests.Services;

using Microsoft.Extensions.Logging;
using NSubstitute;
using OrderManager.Business.Services;
using OrderManager.Core.Entities;
using OrderManager.Core.Ports;
using OrderManager.Core.Requests;

/// <summary>
/// Unit tests for <see cref="ManufacturingService"/>.
/// All data ports and the exporter are substituted so that only service logic is exercised.
/// </summary>
[TestClass]
public sealed class ManufacturingServiceTests
{
    #region Fields

    /// <summary>The substituted order data port.</summary>
    private IOrderDataPort _orderData = null!;

    /// <summary>The substituted board data port.</summary>
    private IBoardDataPort _boardData = null!;

    /// <summary>The substituted component data port.</summary>
    private IComponentDataPort _componentData = null!;

    /// <summary>The substituted production line exporter.</summary>
    private IProductionLineExporter _exporter = null!;

    /// <summary>The system under test.</summary>
    private ManufacturingService _testObject = null!;

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets the MSTest context for the current test.
    /// </summary>
    public TestContext TestContext { get; set; } = null!;

    #endregion

    #region Test Lifecycle

    /// <summary>
    /// Creates fresh substitutes and a new <see cref="ManufacturingService"/> before each test.
    /// </summary>
    [TestInitialize]
    public void Initialize()
    {
        _orderData = Substitute.For<IOrderDataPort>();
        _boardData = Substitute.For<IBoardDataPort>();
        _componentData = Substitute.For<IComponentDataPort>();
        _exporter = Substitute.For<IProductionLineExporter>();
        ILogger<ManufacturingService> logger = Substitute.For<ILogger<ManufacturingService>>();
        _testObject = new ManufacturingService(_orderData, _boardData, _componentData, _exporter, logger);
    }

    #endregion

    #region CreateOrderAsync

    /// <summary>
    /// Returns a new order with a non-empty ID when the request is valid.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [TestMethod]
    public async Task WhenCreateOrderAsync_ThenOrderWithNewIdIsReturned()
    {
        // Arrange.
        CreateOrderRequest request = new("Test Order", "Desc", DateTimeOffset.UtcNow);

        // Act.
        Order result = await _testObject.CreateOrderAsync(request, TestContext.CancellationToken);

        // Assert.
        Assert.AreNotEqual(Guid.Empty, result.Id);
        Assert.AreEqual("Test Order", result.Name);
    }

    /// <summary>
    /// Calls the data port to persist the new order.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [TestMethod]
    public async Task WhenCreateOrderAsync_ThenOrderIsPersisted()
    {
        // Arrange.
        CreateOrderRequest request = new("Order", "D", DateTimeOffset.UtcNow);

        // Act.
        await _testObject.CreateOrderAsync(request, TestContext.CancellationToken);

        // Assert.
        await _orderData.Received(1).AddOrderAsync(Arg.Any<Order>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region DeleteOrderAsync

    /// <summary>
    /// Returns <see langword="false"/> and does not call the data port when the order does not exist.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [TestMethod]
    public async Task WhenDeleteOrderAsyncAndOrderNotFound_ThenFalseIsReturned()
    {
        // Arrange.
        Guid id = Guid.NewGuid();
        _orderData.DeleteOrderAsync(id, Arg.Any<CancellationToken>()).Returns(false);

        // Act.
        bool result = await _testObject.DeleteOrderAsync(id, TestContext.CancellationToken);

        // Assert.
        Assert.IsFalse(result);
    }

    /// <summary>
    /// Deletes the order and returns <see langword="true"/> when the order exists.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [TestMethod]
    public async Task WhenDeleteOrderAsyncAndOrderExists_ThenOrderIsDeletedAndTrueIsReturned()
    {
        // Arrange.
        Guid id = Guid.NewGuid();
        _orderData.DeleteOrderAsync(id, Arg.Any<CancellationToken>()).Returns(true);

        // Act.
        bool result = await _testObject.DeleteOrderAsync(id, TestContext.CancellationToken);

        // Assert.
        Assert.IsTrue(result);
        await _orderData.Received(1).DeleteOrderAsync(id, Arg.Any<CancellationToken>());
    }

    #endregion

    #region ExportToProductionLineAsync

    /// <summary>
    /// Throws <see cref="KeyNotFoundException"/> and does not call the exporter when the order does not exist.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [TestMethod]
    public async Task WhenExportToProductionLineAsyncAndOrderNotFound_ThenKeyNotFoundExceptionIsThrown()
    {
        // Arrange.
        Guid id = Guid.NewGuid();
        _orderData.GetOrderWithDetailsAsync(id, Arg.Any<CancellationToken>()).Returns((Order?)null);

        // Act & Assert.
        await Assert.ThrowsExactlyAsync<KeyNotFoundException>(() => _testObject.ExportToProductionLineAsync(id, TestContext.CancellationToken));
        await _exporter.DidNotReceive().ExportAsync(Arg.Any<Order>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Calls the exporter with the loaded order when the order exists.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [TestMethod]
    public async Task WhenExportToProductionLineAsyncAndOrderExists_ThenExporterIsCalled()
    {
        // Arrange.
        Guid id = Guid.NewGuid();
        Order order = new(id, "O", "D", DateTimeOffset.UtcNow);
        _orderData.GetOrderWithDetailsAsync(id, Arg.Any<CancellationToken>()).Returns(order);

        // Act.
        await _testObject.ExportToProductionLineAsync(id, TestContext.CancellationToken);

        // Assert.
        await _exporter.Received(1).ExportAsync(order, Arg.Any<CancellationToken>());
    }

    #endregion

    #region AddBoardToOrderAsync

    /// <summary>
    /// Returns <see langword="false"/> without touching the data port when the order does not exist.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [TestMethod]
    public async Task WhenAddBoardToOrderAsyncAndOrderNotFound_ThenFalseIsReturned()
    {
        // Arrange.
        Guid orderId = Guid.NewGuid();
        _orderData.GetOrderByIdAsync(orderId, Arg.Any<CancellationToken>()).Returns((Order?)null);

        // Act.
        bool result = await _testObject.AddBoardToOrderAsync(orderId, Guid.NewGuid(), 1, TestContext.CancellationToken);

        // Assert.
        Assert.IsFalse(result);
        await _orderData.DidNotReceive().AddBoardToOrderAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Returns <see langword="false"/> without touching the data port when the board does not exist.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [TestMethod]
    public async Task WhenAddBoardToOrderAsyncAndBoardNotFound_ThenFalseIsReturned()
    {
        // Arrange.
        Guid orderId = Guid.NewGuid();
        Guid boardId = Guid.NewGuid();
        Order order = new(orderId, "O", "D", DateTimeOffset.UtcNow);
        _orderData.GetOrderByIdAsync(orderId, Arg.Any<CancellationToken>()).Returns(order);
        _boardData.GetBoardByIdAsync(boardId, Arg.Any<CancellationToken>()).Returns((Board?)null);

        // Act.
        bool result = await _testObject.AddBoardToOrderAsync(orderId, boardId, 1, TestContext.CancellationToken);

        // Assert.
        Assert.IsFalse(result);
        await _orderData.DidNotReceive().AddBoardToOrderAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Creates the association and returns <see langword="true"/> when both the order and the board exist.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [TestMethod]
    public async Task WhenAddBoardToOrderAsyncAndBothExist_ThenAssociationIsCreatedAndTrueIsReturned()
    {
        // Arrange.
        Guid orderId = Guid.NewGuid();
        Guid boardId = Guid.NewGuid();
        Order order = new(orderId, "O", "D", DateTimeOffset.UtcNow);
        Board board = new(boardId, "B", "D", 100m, 50m);
        _orderData.GetOrderByIdAsync(orderId, Arg.Any<CancellationToken>()).Returns(order);
        _boardData.GetBoardByIdAsync(boardId, Arg.Any<CancellationToken>()).Returns(board);

        // Act.
        bool result = await _testObject.AddBoardToOrderAsync(orderId, boardId, 1, TestContext.CancellationToken);

        // Assert.
        Assert.IsTrue(result);
        await _orderData.Received(1).AddBoardToOrderAsync(orderId, boardId, 1, Arg.Any<CancellationToken>());
    }

    #endregion

    #region RemoveBoardFromOrderAsync

    /// <summary>
    /// Returns <see langword="false"/> without calling the data port when the order does not exist.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [TestMethod]
    public async Task WhenRemoveBoardFromOrderAsyncAndOrderNotFound_ThenFalseIsReturned()
    {
        // Arrange.
        Guid orderId = Guid.NewGuid();
        _orderData.GetOrderByIdAsync(orderId, Arg.Any<CancellationToken>()).Returns((Order?)null);

        // Act.
        bool result = await _testObject.RemoveBoardFromOrderAsync(orderId, Guid.NewGuid(), TestContext.CancellationToken);

        // Assert.
        Assert.IsFalse(result);
        await _orderData.DidNotReceive().RemoveBoardFromOrderAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Returns <see langword="false"/> when the order exists but the board is not associated.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [TestMethod]
    public async Task WhenRemoveBoardFromOrderAsyncAndBoardNotAssociated_ThenFalseIsReturned()
    {
        // Arrange.
        Guid orderId = Guid.NewGuid();
        Guid boardId = Guid.NewGuid();
        Order order = new(orderId, "O", "D", DateTimeOffset.UtcNow);
        _orderData.GetOrderByIdAsync(orderId, Arg.Any<CancellationToken>()).Returns(order);
        _orderData.RemoveBoardFromOrderAsync(orderId, boardId, Arg.Any<CancellationToken>()).Returns(false);

        // Act.
        bool result = await _testObject.RemoveBoardFromOrderAsync(orderId, boardId, TestContext.CancellationToken);

        // Assert.
        Assert.IsFalse(result);
    }

    /// <summary>
    /// Removes the association and returns <see langword="true"/> when the association exists.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [TestMethod]
    public async Task WhenRemoveBoardFromOrderAsyncAndAssociationExists_ThenAssociationIsRemovedAndTrueIsReturned()
    {
        // Arrange.
        Guid orderId = Guid.NewGuid();
        Guid boardId = Guid.NewGuid();
        Order order = new(orderId, "O", "D", DateTimeOffset.UtcNow);
        _orderData.GetOrderByIdAsync(orderId, Arg.Any<CancellationToken>()).Returns(order);
        _orderData.RemoveBoardFromOrderAsync(orderId, boardId, Arg.Any<CancellationToken>()).Returns(true);

        // Act.
        bool result = await _testObject.RemoveBoardFromOrderAsync(orderId, boardId, TestContext.CancellationToken);

        // Assert.
        Assert.IsTrue(result);
        await _orderData.Received(1).RemoveBoardFromOrderAsync(orderId, boardId, Arg.Any<CancellationToken>());
    }

    #endregion

    #region AddComponentToBoardAsync

    /// <summary>
    /// Returns <see langword="false"/> without touching the data port when the board does not exist.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [TestMethod]
    public async Task WhenAddComponentToBoardAsyncAndBoardNotFound_ThenFalseIsReturned()
    {
        // Arrange.
        Guid boardId = Guid.NewGuid();
        _boardData.GetBoardByIdAsync(boardId, Arg.Any<CancellationToken>()).Returns((Board?)null);

        // Act.
        bool result = await _testObject.AddComponentToBoardAsync(boardId, Guid.NewGuid(), 3, TestContext.CancellationToken);

        // Assert.
        Assert.IsFalse(result);
        await _boardData.DidNotReceive().AddComponentToBoardAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Returns <see langword="false"/> without touching the data port when the component does not exist.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [TestMethod]
    public async Task WhenAddComponentToBoardAsyncAndComponentNotFound_ThenFalseIsReturned()
    {
        // Arrange.
        Guid boardId = Guid.NewGuid();
        Guid componentId = Guid.NewGuid();
        Board board = new(boardId, "B", "D", 100m, 50m);
        _boardData.GetBoardByIdAsync(boardId, Arg.Any<CancellationToken>()).Returns(board);
        _componentData.GetComponentByIdAsync(componentId, Arg.Any<CancellationToken>()).Returns((Component?)null);

        // Act.
        bool result = await _testObject.AddComponentToBoardAsync(boardId, componentId, 3, TestContext.CancellationToken);

        // Assert.
        Assert.IsFalse(result);
        await _boardData.DidNotReceive().AddComponentToBoardAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Creates the association with the given quantity and returns <see langword="true"/> when both entities exist.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [TestMethod]
    public async Task WhenAddComponentToBoardAsyncAndBothExist_ThenAssociationIsCreatedAndTrueIsReturned()
    {
        // Arrange.
        Guid boardId = Guid.NewGuid();
        Guid componentId = Guid.NewGuid();
        Board board = new(boardId, "B", "D", 100m, 50m);
        Component component = new(componentId, "C", "D", 10);
        _boardData.GetBoardByIdAsync(boardId, Arg.Any<CancellationToken>()).Returns(board);
        _componentData.GetComponentByIdAsync(componentId, Arg.Any<CancellationToken>()).Returns(component);

        // Act.
        bool result = await _testObject.AddComponentToBoardAsync(boardId, componentId, 5, TestContext.CancellationToken);

        // Assert.
        Assert.IsTrue(result);
        await _boardData.Received(1).AddComponentToBoardAsync(boardId, componentId, 5, Arg.Any<CancellationToken>());
    }

    #endregion

    #region DeleteBoardAsync

    /// <summary>
    /// Returns <see langword="false"/> and does not call the data port when the board does not exist.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [TestMethod]
    public async Task WhenDeleteBoardAsyncAndBoardNotFound_ThenFalseIsReturned()
    {
        // Arrange.
        Guid id = Guid.NewGuid();
        _boardData.GetBoardByIdAsync(id, Arg.Any<CancellationToken>()).Returns((Board?)null);

        // Act.
        bool result = await _testObject.DeleteBoardAsync(id, TestContext.CancellationToken);

        // Assert.
        Assert.IsFalse(result);
        await _boardData.DidNotReceive().DeleteBoardAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Throws <see cref="InvalidOperationException"/> and does not delete when the board is still assigned to an order.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [TestMethod]
    public async Task WhenDeleteBoardAsyncAndBoardIsAssignedToOrder_ThenInvalidOperationExceptionIsThrown()
    {
        // Arrange.
        Guid id = Guid.NewGuid();
        Board board = new(id, "B", "D", 100m, 50m);
        _boardData.GetBoardByIdAsync(id, Arg.Any<CancellationToken>()).Returns(board);
        _boardData.IsBoardAssignedToAnyOrderAsync(id, Arg.Any<CancellationToken>()).Returns(true);

        // Act & Assert.
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => _testObject.DeleteBoardAsync(id, TestContext.CancellationToken));
        await _boardData.DidNotReceive().DeleteBoardAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Deletes the board and returns <see langword="true"/> when the board exists and is not assigned to any order.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [TestMethod]
    public async Task WhenDeleteBoardAsyncAndBoardExistsAndNotAssigned_ThenBoardIsDeletedAndTrueIsReturned()
    {
        // Arrange.
        Guid id = Guid.NewGuid();
        Board board = new(id, "B", "D", 100m, 50m);
        _boardData.GetBoardByIdAsync(id, Arg.Any<CancellationToken>()).Returns(board);
        _boardData.IsBoardAssignedToAnyOrderAsync(id, Arg.Any<CancellationToken>()).Returns(false);
        _boardData.DeleteBoardAsync(id, Arg.Any<CancellationToken>()).Returns(true);

        // Act.
        bool result = await _testObject.DeleteBoardAsync(id, TestContext.CancellationToken);

        // Assert.
        Assert.IsTrue(result);
        await _boardData.Received(1).DeleteBoardAsync(id, Arg.Any<CancellationToken>());
    }

    #endregion

    #region DeleteComponentAsync

    /// <summary>
    /// Returns <see langword="false"/> and does not call the data port when the component does not exist.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [TestMethod]
    public async Task WhenDeleteComponentAsyncAndComponentNotFound_ThenFalseIsReturned()
    {
        // Arrange.
        Guid id = Guid.NewGuid();
        _componentData.GetComponentByIdAsync(id, Arg.Any<CancellationToken>()).Returns((Component?)null);

        // Act.
        bool result = await _testObject.DeleteComponentAsync(id, TestContext.CancellationToken);

        // Assert.
        Assert.IsFalse(result);
        await _componentData.DidNotReceive().DeleteComponentAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Throws <see cref="InvalidOperationException"/> and does not delete when the component is still assigned to a board.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [TestMethod]
    public async Task WhenDeleteComponentAsyncAndComponentIsAssignedToBoard_ThenInvalidOperationExceptionIsThrown()
    {
        // Arrange.
        Guid id = Guid.NewGuid();
        Component component = new(id, "C", "D", 10);
        _componentData.GetComponentByIdAsync(id, Arg.Any<CancellationToken>()).Returns(component);
        _componentData.IsComponentAssignedToAnyBoardAsync(id, Arg.Any<CancellationToken>()).Returns(true);

        // Act & Assert.
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => _testObject.DeleteComponentAsync(id, TestContext.CancellationToken));
        await _componentData.DidNotReceive().DeleteComponentAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Deletes the component and returns <see langword="true"/> when the component exists and is not assigned to any board.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [TestMethod]
    public async Task WhenDeleteComponentAsyncAndComponentExistsAndNotAssigned_ThenComponentIsDeletedAndTrueIsReturned()
    {
        // Arrange.
        Guid id = Guid.NewGuid();
        Component component = new(id, "C", "D", 10);
        _componentData.GetComponentByIdAsync(id, Arg.Any<CancellationToken>()).Returns(component);
        _componentData.IsComponentAssignedToAnyBoardAsync(id, Arg.Any<CancellationToken>()).Returns(false);
        _componentData.DeleteComponentAsync(id, Arg.Any<CancellationToken>()).Returns(true);

        // Act.
        bool result = await _testObject.DeleteComponentAsync(id, TestContext.CancellationToken);

        // Assert.
        Assert.IsTrue(result);
        await _componentData.Received(1).DeleteComponentAsync(id, Arg.Any<CancellationToken>());
    }

    #endregion

    #region UpdateOrderAsync

    /// <summary>
    /// Returns <see langword="null"/> and does not call the data port when the order does not exist.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [TestMethod]
    public async Task WhenUpdateOrderAsyncAndOrderNotFound_ThenNullIsReturned()
    {
        // Arrange.
        Guid id = Guid.NewGuid();
        _orderData.GetOrderByIdAsync(id, Arg.Any<CancellationToken>()).Returns((Order?)null);

        // Act.
        Order? result = await _testObject.UpdateOrderAsync(id, new UpdateOrderRequest("New", "Desc"), TestContext.CancellationToken);

        // Assert.
        Assert.IsNull(result);
        await _orderData.DidNotReceive().UpdateOrderAsync(Arg.Any<Order>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Returns the updated order with the new values applied when the order exists.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [TestMethod]
    public async Task WhenUpdateOrderAsyncAndOrderExists_ThenUpdatedOrderIsReturned()
    {
        // Arrange.
        Guid id = Guid.NewGuid();
        Order existing = new(id, "Old", "OldDesc", DateTimeOffset.UtcNow);
        _orderData.GetOrderByIdAsync(id, Arg.Any<CancellationToken>()).Returns(existing);

        // Act.
        Order? result = await _testObject.UpdateOrderAsync(id, new UpdateOrderRequest("New", "NewDesc"), TestContext.CancellationToken);

        // Assert.
        Assert.IsNotNull(result);
        Assert.AreEqual("New", result.Name);
        Assert.AreEqual("NewDesc", result.Description);
        await _orderData.Received(1).UpdateOrderAsync(Arg.Is<Order>(o => o.Name == "New"), Arg.Any<CancellationToken>());
    }

    #endregion
}