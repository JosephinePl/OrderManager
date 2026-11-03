namespace OrderManager.Business.Services;

using Microsoft.Extensions.Logging;
using OrderManager.Core.Entities;
using OrderManager.Core.Ports;
using OrderManager.Core.Requests;

/// <summary>
/// Application service for managing manufacturing entities: orders, boards, and components.
/// </summary>
public sealed partial class ManufacturingService : IManufacturingService
{
    #region Fields

    /// <summary>The driven data port for orders.</summary>
    private readonly IOrderDataPort _orderData;

    /// <summary>The driven data port for boards.</summary>
    private readonly IBoardDataPort _boardData;

    /// <summary>The driven data port for components.</summary>
    private readonly IComponentDataPort _componentData;

    /// <summary>The production line export adapter.</summary>
    private readonly IProductionLineExporter _exporter;

    /// <summary>The logger.</summary>
    private readonly ILogger<ManufacturingService> _logger;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="ManufacturingService"/> class.
    /// </summary>
    /// <param name="orderData">The driven data port for orders.</param>
    /// <param name="boardData">The driven data port for boards.</param>
    /// <param name="componentData">The driven data port for components.</param>
    /// <param name="exporter">The production line export adapter.</param>
    /// <param name="logger">The logger.</param>
    public ManufacturingService(
        IOrderDataPort orderData,
        IBoardDataPort boardData,
        IComponentDataPort componentData,
        IProductionLineExporter exporter,
        ILogger<ManufacturingService> logger)
    {
        ArgumentNullException.ThrowIfNull(orderData);
        ArgumentNullException.ThrowIfNull(boardData);
        ArgumentNullException.ThrowIfNull(componentData);
        ArgumentNullException.ThrowIfNull(exporter);
        ArgumentNullException.ThrowIfNull(logger);
        _orderData = orderData;
        _boardData = boardData;
        _componentData = componentData;
        _exporter = exporter;
        _logger = logger;
    }

    #endregion

    #region Orders

    /// <inheritdoc/>
    public async Task<Order> CreateOrderAsync(CreateOrderRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        Order order = new Order(Guid.NewGuid(), request.Name, request.Description, request.OrderDate);
        await _orderData.AddOrderAsync(order, cancellationToken);
        LogOrderCreated(_logger, order.Id, order.Name);
        return order;
    }

    /// <inheritdoc/>
    public async Task<Order?> GetOrderByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _orderData.GetOrderByIdAsync(id, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Order>> GetAllOrdersAsync(CancellationToken cancellationToken = default)
    {
        return await _orderData.GetAllOrdersAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Order?> UpdateOrderAsync(Guid id, UpdateOrderRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        Order? existing = await _orderData.GetOrderByIdAsync(id, cancellationToken);
        if (existing is null)
        {
            return null;
        }

        Order updated = existing with { Name = request.Name, Description = request.Description };
        await _orderData.UpdateOrderAsync(updated, cancellationToken);
        LogOrderUpdated(_logger, id);
        return updated;
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteOrderAsync(Guid id, CancellationToken cancellationToken = default)
    {
        Order? existing = await _orderData.GetOrderByIdAsync(id, cancellationToken);
        if (existing is null)
        {
            return false;
        }

        await _orderData.DeleteOrderAsync(id, cancellationToken);
        LogOrderDeleted(_logger, id);
        return true;
    }

    /// <inheritdoc/>
    public async Task ExportToProductionLineAsync(Guid id, CancellationToken cancellationToken = default)
    {
        Order? order = await _orderData.GetOrderWithDetailsAsync(id, cancellationToken);
        if (order is null)
        {
            throw new KeyNotFoundException($"Order with ID '{id}' was not found.");
        }

        await _exporter.ExportAsync(order, cancellationToken);
        LogOrderExported(_logger, id);
    }

    /// <inheritdoc/>
    public async Task<bool> AddBoardToOrderAsync(Guid orderId, Guid boardId, CancellationToken cancellationToken = default)
    {
        Order? order = await _orderData.GetOrderByIdAsync(orderId, cancellationToken);
        if (order is null)
        {
            return false;
        }

        Board? board = await _boardData.GetBoardByIdAsync(boardId, cancellationToken);
        if (board is null)
        {
            return false;
        }

        await _orderData.AddBoardToOrderAsync(orderId, boardId, cancellationToken);
        LogBoardAddedToOrder(_logger, boardId, orderId);
        return true;
    }

    /// <inheritdoc/>
    public async Task<bool> RemoveBoardFromOrderAsync(Guid orderId, Guid boardId, CancellationToken cancellationToken = default)
    {
        Order? order = await _orderData.GetOrderByIdAsync(orderId, cancellationToken);
        if (order is null)
        {
            return false;
        }

        bool removed = await _orderData.RemoveBoardFromOrderAsync(orderId, boardId, cancellationToken);
        if (removed)
        {
            LogBoardRemovedFromOrder(_logger, boardId, orderId);
        }

        return removed;
    }

    #endregion

    #region Boards

    /// <inheritdoc/>
    public async Task<Board> CreateBoardAsync(CreateBoardRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        Board board = new Board(Guid.NewGuid(), request.Name, request.Description, request.Length, request.Width);
        await _boardData.AddBoardAsync(board, cancellationToken);
        LogBoardCreated(_logger, board.Id, board.Name);
        return board;
    }

    /// <inheritdoc/>
    public async Task<Board?> GetBoardByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _boardData.GetBoardByIdAsync(id, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Board>> GetAllBoardsAsync(CancellationToken cancellationToken = default)
    {
        return await _boardData.GetAllBoardsAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Board?> UpdateBoardAsync(Guid id, UpdateBoardRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        Board? existing = await _boardData.GetBoardByIdAsync(id, cancellationToken);
        if (existing is null)
        {
            return null;
        }

        Board updated = existing with { Name = request.Name, Description = request.Description, Length = request.Length, Width = request.Width };
        await _boardData.UpdateBoardAsync(updated, cancellationToken);
        LogBoardUpdated(_logger, id);
        return updated;
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteBoardAsync(Guid id, CancellationToken cancellationToken = default)
    {
        Board? existing = await _boardData.GetBoardByIdAsync(id, cancellationToken);
        if (existing is null)
        {
            return false;
        }

        bool isAssigned = await _boardData.IsBoardAssignedToAnyOrderAsync(id, cancellationToken);
        if (isAssigned)
        {
            throw new InvalidOperationException($"Board '{id}' cannot be deleted because it is still assigned to one or more orders.");
        }

        await _boardData.DeleteBoardAsync(id, cancellationToken);
        LogBoardDeleted(_logger, id);
        return true;
    }

    /// <inheritdoc/>
    public async Task<bool> AddComponentToBoardAsync(Guid boardId, Guid componentId, int quantityPerBoard, CancellationToken cancellationToken = default)
    {
        Board? board = await _boardData.GetBoardByIdAsync(boardId, cancellationToken);
        if (board is null)
        {
            return false;
        }

        Component? component = await _componentData.GetComponentByIdAsync(componentId, cancellationToken);
        if (component is null)
        {
            return false;
        }

        await _boardData.AddComponentToBoardAsync(boardId, componentId, quantityPerBoard, cancellationToken);
        LogComponentAddedToBoard(_logger, componentId, boardId);
        return true;
    }

    /// <inheritdoc/>
    public async Task<bool> RemoveComponentFromBoardAsync(Guid boardId, Guid componentId, CancellationToken cancellationToken = default)
    {
        Board? board = await _boardData.GetBoardByIdAsync(boardId, cancellationToken);
        if (board is null)
        {
            return false;
        }

        bool removed = await _boardData.RemoveComponentFromBoardAsync(boardId, componentId, cancellationToken);
        if (removed)
        {
            LogComponentRemovedFromBoard(_logger, componentId, boardId);
        }

        return removed;
    }

    #endregion

    #region Components

    /// <inheritdoc/>
    public async Task<Component> CreateComponentAsync(CreateComponentRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        Component component = new Component(Guid.NewGuid(), request.Name, request.Description, request.Quantity);
        await _componentData.AddComponentAsync(component, cancellationToken);
        LogComponentCreated(_logger, component.Id, component.Name);
        return component;
    }

    /// <inheritdoc/>
    public async Task<Component?> GetComponentByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _componentData.GetComponentByIdAsync(id, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Component>> GetAllComponentsAsync(CancellationToken cancellationToken = default)
    {
        return await _componentData.GetAllComponentsAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Component?> UpdateComponentAsync(Guid id, UpdateComponentRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        Component? existing = await _componentData.GetComponentByIdAsync(id, cancellationToken);
        if (existing is null)
        {
            return null;
        }

        Component updated = existing with { Name = request.Name, Description = request.Description, Quantity = request.Quantity };
        await _componentData.UpdateComponentAsync(updated, cancellationToken);
        LogComponentUpdated(_logger, id);
        return updated;
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteComponentAsync(Guid id, CancellationToken cancellationToken = default)
    {
        Component? existing = await _componentData.GetComponentByIdAsync(id, cancellationToken);
        if (existing is null)
        {
            return false;
        }

        bool isAssigned = await _componentData.IsComponentAssignedToAnyBoardAsync(id, cancellationToken);
        if (isAssigned)
        {
            throw new InvalidOperationException($"Component '{id}' cannot be deleted because it is still assigned to one or more boards.");
        }

        await _componentData.DeleteComponentAsync(id, cancellationToken);
        LogComponentDeleted(_logger, id);
        return true;
    }

    #endregion

    #region Logging

    /// <summary>Logs the creation of an order.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="orderId">The ID of the created order.</param>
    /// <param name="orderName">The name of the created order.</param>
    [LoggerMessage(Level = LogLevel.Information, Message = "Created order {OrderId} ({OrderName}).")]
    private static partial void LogOrderCreated(ILogger logger, Guid orderId, string orderName);

    /// <summary>Logs the update of an order.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="orderId">The ID of the updated order.</param>
    [LoggerMessage(Level = LogLevel.Information, Message = "Updated order {OrderId}.")]
    private static partial void LogOrderUpdated(ILogger logger, Guid orderId);

    /// <summary>Logs the deletion of an order.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="orderId">The ID of the deleted order.</param>
    [LoggerMessage(Level = LogLevel.Information, Message = "Deleted order {OrderId}.")]
    private static partial void LogOrderDeleted(ILogger logger, Guid orderId);

    /// <summary>Logs the export of an order to the production line.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="orderId">The ID of the exported order.</param>
    [LoggerMessage(Level = LogLevel.Information, Message = "Exported order {OrderId} to production line.")]
    private static partial void LogOrderExported(ILogger logger, Guid orderId);

    /// <summary>Logs the addition of a board to an order.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="boardId">The ID of the board added.</param>
    /// <param name="orderId">The ID of the order.</param>
    [LoggerMessage(Level = LogLevel.Information, Message = "Added board {BoardId} to order {OrderId}.")]
    private static partial void LogBoardAddedToOrder(ILogger logger, Guid boardId, Guid orderId);

    /// <summary>Logs the removal of a board from an order.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="boardId">The ID of the board removed.</param>
    /// <param name="orderId">The ID of the order.</param>
    [LoggerMessage(Level = LogLevel.Information, Message = "Removed board {BoardId} from order {OrderId}.")]
    private static partial void LogBoardRemovedFromOrder(ILogger logger, Guid boardId, Guid orderId);

    /// <summary>Logs the creation of a board.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="boardId">The ID of the created board.</param>
    /// <param name="boardName">The name of the created board.</param>
    [LoggerMessage(Level = LogLevel.Information, Message = "Created board {BoardId} ({BoardName}).")]
    private static partial void LogBoardCreated(ILogger logger, Guid boardId, string boardName);

    /// <summary>Logs the update of a board.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="boardId">The ID of the updated board.</param>
    [LoggerMessage(Level = LogLevel.Information, Message = "Updated board {BoardId}.")]
    private static partial void LogBoardUpdated(ILogger logger, Guid boardId);

    /// <summary>Logs the deletion of a board.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="boardId">The ID of the deleted board.</param>
    [LoggerMessage(Level = LogLevel.Information, Message = "Deleted board {BoardId}.")]
    private static partial void LogBoardDeleted(ILogger logger, Guid boardId);

    /// <summary>Logs the addition of a component to a board.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="componentId">The ID of the component added.</param>
    /// <param name="boardId">The ID of the board.</param>
    [LoggerMessage(Level = LogLevel.Information, Message = "Added component {ComponentId} to board {BoardId}.")]
    private static partial void LogComponentAddedToBoard(ILogger logger, Guid componentId, Guid boardId);

    /// <summary>Logs the removal of a component from a board.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="componentId">The ID of the component removed.</param>
    /// <param name="boardId">The ID of the board.</param>
    [LoggerMessage(Level = LogLevel.Information, Message = "Removed component {ComponentId} from board {BoardId}.")]
    private static partial void LogComponentRemovedFromBoard(ILogger logger, Guid componentId, Guid boardId);

    /// <summary>Logs the creation of a component.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="componentId">The ID of the created component.</param>
    /// <param name="componentName">The name of the created component.</param>
    [LoggerMessage(Level = LogLevel.Information, Message = "Created component {ComponentId} ({ComponentName}).")]
    private static partial void LogComponentCreated(ILogger logger, Guid componentId, string componentName);

    /// <summary>Logs the update of a component.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="componentId">The ID of the updated component.</param>
    [LoggerMessage(Level = LogLevel.Information, Message = "Updated component {ComponentId}.")]
    private static partial void LogComponentUpdated(ILogger logger, Guid componentId);

    /// <summary>Logs the deletion of a component.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="componentId">The ID of the deleted component.</param>
    [LoggerMessage(Level = LogLevel.Information, Message = "Deleted component {ComponentId}.")]
    private static partial void LogComponentDeleted(ILogger logger, Guid componentId);

    #endregion
}