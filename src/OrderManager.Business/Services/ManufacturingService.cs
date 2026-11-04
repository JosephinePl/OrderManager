namespace OrderManager.Business.Services;

using Microsoft.Extensions.Logging;
using OrderManager.Core.Entities;
using OrderManager.Core.Exceptions;
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

    /// <summary>
    /// Creates a new order from the given request data.
    /// </summary>
    /// <param name="request">The data for the new order.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The newly created <see cref="Order"/>.</returns>
    public async Task<Order> CreateOrderAsync(CreateOrderRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        Order order = new Order(Guid.NewGuid(), request.Name, request.Description, request.OrderDate);
        await _orderData.AddOrderAsync(order, cancellationToken);
        LogOrderCreated(_logger, order.Id, order.Name);
        return order;
    }

    /// <summary>
    /// Gets the order with the given ID, or <see langword="null"/> if not found.
    /// </summary>
    /// <param name="id">The ID of the order to retrieve.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The matching <see cref="Order"/>, or <see langword="null"/> if not found.</returns>
    public async Task<Order?> GetOrderByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _orderData.GetOrderWithDetailsAsync(id, cancellationToken);
    }

    /// <summary>
    /// Gets all orders, optionally filtered by a case-insensitive name substring.
    /// </summary>
    /// <param name="nameFilter">When non-null and non-empty, only orders whose name contains this value are returned.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>An enumerable of matching <see cref="Order"/> instances.</returns>
    public async Task<IEnumerable<Order>> GetAllOrdersAsync(string? nameFilter = null, CancellationToken cancellationToken = default)
    {
        return await _orderData.GetAllOrdersAsync(nameFilter, cancellationToken);
    }

    /// <summary>
    /// Updates the order with the given ID using the provided request data.
    /// </summary>
    /// <param name="id">The ID of the order to update.</param>
    /// <param name="request">The updated data for the order.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The updated <see cref="Order"/>, or <see langword="null"/> if no order with the given ID exists.</returns>
    public async Task<Order?> UpdateOrderAsync(Guid id, UpdateOrderRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        Order? result;
        Order? existing = await _orderData.GetOrderByIdAsync(id, cancellationToken);
        if (existing is null)
        {
            result = null;
        }
        else
        {
            Order updated = existing with { Name = request.Name, Description = request.Description };
            await _orderData.UpdateOrderAsync(updated, cancellationToken);
            LogOrderUpdated(_logger, id);
            result = updated;
        }

        return result;
    }

    /// <summary>
    /// Deletes the order with the given ID.
    /// </summary>
    /// <param name="id">The ID of the order to delete.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// <see langword="true"/> if the order was deleted;
    /// <see langword="false"/> if no order with the given ID was found.
    /// </returns>
    public async Task<bool> DeleteOrderAsync(Guid id, CancellationToken cancellationToken = default)
    {
        bool deleted = await _orderData.DeleteOrderAsync(id, cancellationToken);
        if (deleted)
        {
            LogOrderDeleted(_logger, id);
        }

        return deleted;
    }

    /// <summary>
    /// Exports the order with the given ID to the production line.
    /// </summary>
    /// <param name="id">The ID of the order to export.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when no order with the given ID exists.</exception>
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

    /// <summary>
    /// Adds a board to the order with the given ID, or updates its quantity if already present.
    /// </summary>
    /// <param name="orderId">The ID of the order.</param>
    /// <param name="boardId">The ID of the board to add.</param>
    /// <param name="quantity">The number of this board required for the order.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// <see langword="true"/> if the board was added or updated;
    /// <see langword="false"/> if the order or board was not found.
    /// </returns>
    public async Task<bool> AddBoardToOrderAsync(Guid orderId, Guid boardId, int quantity, CancellationToken cancellationToken = default)
    {
        bool result;
        Order? order = await _orderData.GetOrderByIdAsync(orderId, cancellationToken);
        if (order is null)
        {
            result = false;
        }
        else
        {
            Board? board = await _boardData.GetBoardByIdAsync(boardId, cancellationToken);
            if (board is null)
            {
                result = false;
            }
            else
            {
                try
                {
                    await _orderData.AddBoardToOrderAsync(orderId, boardId, quantity, cancellationToken);
                    LogBoardAddedToOrder(_logger, boardId, orderId);
                    result = true;
                }
                catch (NotFoundException)
                {
                    LogBoardToOrderRaceCondition(_logger, boardId, orderId);
                    result = false;
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Removes a board from the order with the given ID.
    /// </summary>
    /// <param name="orderId">The ID of the order.</param>
    /// <param name="boardId">The ID of the board to remove.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// <see langword="true"/> if the board was removed;
    /// <see langword="false"/> if the order was not found or the board was not part of the order.
    /// </returns>
    public async Task<bool> RemoveBoardFromOrderAsync(Guid orderId, Guid boardId, CancellationToken cancellationToken = default)
    {
        bool result;
        Order? order = await _orderData.GetOrderByIdAsync(orderId, cancellationToken);
        if (order is null)
        {
            result = false;
        }
        else
        {
            bool removed = await _orderData.RemoveBoardFromOrderAsync(orderId, boardId, cancellationToken);
            if (removed)
            {
                LogBoardRemovedFromOrder(_logger, boardId, orderId);
            }

            result = removed;
        }

        return result;
    }

    #endregion

    #region Boards

    /// <summary>
    /// Creates a new board from the given request data.
    /// </summary>
    /// <param name="request">The data for the new board.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The newly created <see cref="Board"/>.</returns>
    public async Task<Board> CreateBoardAsync(CreateBoardRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        Board board = new Board(Guid.NewGuid(), request.Name, request.Description, request.Length, request.Width);
        await _boardData.AddBoardAsync(board, cancellationToken);
        LogBoardCreated(_logger, board.Id, board.Name);
        return board;
    }

    /// <summary>
    /// Gets the board with the given ID, or <see langword="null"/> if not found.
    /// </summary>
    /// <param name="id">The ID of the board to retrieve.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The matching <see cref="Board"/>, or <see langword="null"/> if not found.</returns>
    public async Task<Board?> GetBoardByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _boardData.GetBoardWithDetailsAsync(id, cancellationToken);
    }

    /// <summary>
    /// Gets all boards, optionally filtered by a case-insensitive name substring.
    /// </summary>
    /// <param name="nameFilter">When non-null and non-empty, only boards whose name contains this value are returned.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>An enumerable of matching <see cref="Board"/> instances.</returns>
    public async Task<IEnumerable<Board>> GetAllBoardsAsync(string? nameFilter = null, CancellationToken cancellationToken = default)
    {
        return await _boardData.GetAllBoardsAsync(nameFilter, cancellationToken);
    }

    /// <summary>
    /// Updates the board with the given ID using the provided request data.
    /// </summary>
    /// <param name="id">The ID of the board to update.</param>
    /// <param name="request">The updated data for the board.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The updated <see cref="Board"/>, or <see langword="null"/> if no board with the given ID exists.</returns>
    public async Task<Board?> UpdateBoardAsync(Guid id, UpdateBoardRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        Board? result;
        Board? existing = await _boardData.GetBoardByIdAsync(id, cancellationToken);
        if (existing is null)
        {
            result = null;
        }
        else
        {
            Board updated = existing with { Name = request.Name, Description = request.Description, Length = request.Length, Width = request.Width };
            await _boardData.UpdateBoardAsync(updated, cancellationToken);
            LogBoardUpdated(_logger, id);
            result = updated;
        }

        return result;
    }

    /// <summary>
    /// Deletes the board with the given ID.
    /// </summary>
    /// <param name="id">The ID of the board to delete.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// <see langword="true"/> if the board was deleted;
    /// <see langword="false"/> if no board with the given ID was found.
    /// </returns>
    /// <exception cref="InvalidOperationException">Thrown when the board is still assigned to one or more orders.</exception>
    public async Task<bool> DeleteBoardAsync(Guid id, CancellationToken cancellationToken = default)
    {
        bool result;
        Board? existing = await _boardData.GetBoardByIdAsync(id, cancellationToken);
        if (existing is null)
        {
            result = false;
        }
        else
        {
            bool isAssigned = await _boardData.IsBoardAssignedToAnyOrderAsync(id, cancellationToken);
            if (isAssigned)
            {
                throw new InvalidOperationException($"Board '{id}' cannot be deleted because it is still assigned to one or more orders.");
            }

            result = await _boardData.DeleteBoardAsync(id, cancellationToken);
            if (result)
            {
                LogBoardDeleted(_logger, id);
            }
        }

        return result;
    }

    /// <summary>
    /// Adds a component to the board with the given ID, or updates its quantity if already present.
    /// </summary>
    /// <param name="boardId">The ID of the board.</param>
    /// <param name="componentId">The ID of the component to add.</param>
    /// <param name="quantityPerBoard">The number of this component required per board.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// <see langword="true"/> if the component was added or updated;
    /// <see langword="false"/> if the board or component was not found.
    /// </returns>
    public async Task<bool> AddComponentToBoardAsync(Guid boardId, Guid componentId, int quantityPerBoard, CancellationToken cancellationToken = default)
    {
        bool result;
        Board? board = await _boardData.GetBoardByIdAsync(boardId, cancellationToken);
        if (board is null)
        {
            result = false;
        }
        else
        {
            Component? component = await _componentData.GetComponentByIdAsync(componentId, cancellationToken);
            if (component is null)
            {
                result = false;
            }
            else
            {
                try
                {
                    await _boardData.AddComponentToBoardAsync(boardId, componentId, quantityPerBoard, cancellationToken);
                    LogComponentAddedToBoard(_logger, componentId, boardId);
                    result = true;
                }
                catch (NotFoundException)
                {
                    LogComponentToBoardRaceCondition(_logger, componentId, boardId);
                    result = false;
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Removes a component from the board with the given ID.
    /// </summary>
    /// <param name="boardId">The ID of the board.</param>
    /// <param name="componentId">The ID of the component to remove.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// <see langword="true"/> if the component was removed;
    /// <see langword="false"/> if the board was not found or the component was not part of the board.
    /// </returns>
    public async Task<bool> RemoveComponentFromBoardAsync(Guid boardId, Guid componentId, CancellationToken cancellationToken = default)
    {
        bool result;
        Board? board = await _boardData.GetBoardByIdAsync(boardId, cancellationToken);
        if (board is null)
        {
            result = false;
        }
        else
        {
            bool removed = await _boardData.RemoveComponentFromBoardAsync(boardId, componentId, cancellationToken);
            if (removed)
            {
                LogComponentRemovedFromBoard(_logger, componentId, boardId);
            }

            result = removed;
        }

        return result;
    }

    #endregion

    #region Components

    /// <summary>
    /// Creates a new component from the given request data.
    /// </summary>
    /// <param name="request">The data for the new component.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The newly created <see cref="Component"/>.</returns>
    public async Task<Component> CreateComponentAsync(CreateComponentRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        Component component = new Component(Guid.NewGuid(), request.Name, request.Description, request.Quantity);
        await _componentData.AddComponentAsync(component, cancellationToken);
        LogComponentCreated(_logger, component.Id, component.Name);
        return component;
    }

    /// <summary>
    /// Gets the component with the given ID, or <see langword="null"/> if not found.
    /// </summary>
    /// <param name="id">The ID of the component to retrieve.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The matching <see cref="Component"/>, or <see langword="null"/> if not found.</returns>
    public async Task<Component?> GetComponentByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _componentData.GetComponentByIdAsync(id, cancellationToken);
    }

    /// <summary>
    /// Gets all components, optionally filtered by a case-insensitive name substring.
    /// </summary>
    /// <param name="nameFilter">When non-null and non-empty, only components whose name contains this value are returned.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>An enumerable of matching <see cref="Component"/> instances.</returns>
    public async Task<IEnumerable<Component>> GetAllComponentsAsync(string? nameFilter = null, CancellationToken cancellationToken = default)
    {
        return await _componentData.GetAllComponentsAsync(nameFilter, cancellationToken);
    }

    /// <summary>
    /// Updates the component with the given ID using the provided request data.
    /// </summary>
    /// <param name="id">The ID of the component to update.</param>
    /// <param name="request">The updated data for the component.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The updated <see cref="Component"/>, or <see langword="null"/> if no component with the given ID exists.</returns>
    public async Task<Component?> UpdateComponentAsync(Guid id, UpdateComponentRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        Component? result;
        Component? existing = await _componentData.GetComponentByIdAsync(id, cancellationToken);
        if (existing is null)
        {
            result = null;
        }
        else
        {
            Component updated = existing with { Name = request.Name, Description = request.Description, Quantity = request.Quantity };
            await _componentData.UpdateComponentAsync(updated, cancellationToken);
            LogComponentUpdated(_logger, id);
            result = updated;
        }

        return result;
    }

    /// <summary>
    /// Deletes the component with the given ID.
    /// </summary>
    /// <param name="id">The ID of the component to delete.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// <see langword="true"/> if the component was deleted;
    /// <see langword="false"/> if no component with the given ID was found.
    /// </returns>
    /// <exception cref="InvalidOperationException">Thrown when the component is still assigned to one or more boards.</exception>
    public async Task<bool> DeleteComponentAsync(Guid id, CancellationToken cancellationToken = default)
    {
        bool result;
        Component? existing = await _componentData.GetComponentByIdAsync(id, cancellationToken);
        if (existing is null)
        {
            result = false;
        }
        else
        {
            bool isAssigned = await _componentData.IsComponentAssignedToAnyBoardAsync(id, cancellationToken);
            if (isAssigned)
            {
                throw new InvalidOperationException($"Component '{id}' cannot be deleted because it is still assigned to one or more boards.");
            }

            result = await _componentData.DeleteComponentAsync(id, cancellationToken);
            if (result)
            {
                LogComponentDeleted(_logger, id);
            }
        }

        return result;
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

    /// <summary>Logs a race condition where a board or order was deleted between the pre-check and the insert.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="boardId">The ID of the board.</param>
    /// <param name="orderId">The ID of the order.</param>
    [LoggerMessage(Level = LogLevel.Warning, Message = "Board {BoardId} or order {OrderId} was deleted concurrently; add-board-to-order aborted.")]
    private static partial void LogBoardToOrderRaceCondition(ILogger logger, Guid boardId, Guid orderId);

    /// <summary>Logs a race condition where a component or board was deleted between the pre-check and the insert.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="componentId">The ID of the component.</param>
    /// <param name="boardId">The ID of the board.</param>
    [LoggerMessage(Level = LogLevel.Warning, Message = "Component {ComponentId} or board {BoardId} was deleted concurrently; add-component-to-board aborted.")]
    private static partial void LogComponentToBoardRaceCondition(ILogger logger, Guid componentId, Guid boardId);

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