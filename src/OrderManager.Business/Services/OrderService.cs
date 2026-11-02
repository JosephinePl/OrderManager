namespace OrderManager.Business.Services;

using Microsoft.Extensions.Logging;
using OrderManager.Core.Entities;
using OrderManager.Core.Ports;
using OrderManager.Core.Requests;

/// <summary>
/// Application service for managing <see cref="Order"/> entities.
/// </summary>
public sealed partial class OrderService : IOrderService
{
    #region Fields

    /// <summary>The driven data port.</summary>
    private readonly IOrderDataPort _data;

    /// <summary>The board data port, used to verify board existence.</summary>
    private readonly IBoardDataPort _boardData;

    /// <summary>The production line export adapter.</summary>
    private readonly IProductionLineExporter _exporter;

    /// <summary>The logger.</summary>
    private readonly ILogger<OrderService> _logger;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="OrderService"/> class.
    /// </summary>
    /// <param name="data">The driven data port.</param>
    /// <param name="boardData">The board data port, used to verify board existence.</param>
    /// <param name="exporter">The production line export adapter.</param>
    /// <param name="logger">The logger.</param>
    public OrderService(IOrderDataPort data, IBoardDataPort boardData, IProductionLineExporter exporter, ILogger<OrderService> logger)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(boardData);
        ArgumentNullException.ThrowIfNull(exporter);
        ArgumentNullException.ThrowIfNull(logger);
        _data = data;
        _boardData = boardData;
        _exporter = exporter;
        _logger = logger;
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Creates a new order from the given request data.
    /// </summary>
    /// <param name="request">The data for the new order.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The newly created <see cref="Order"/>.</returns>
    public async Task<Order> CreateAsync(CreateOrderRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        Order order = new Order(Guid.NewGuid(), request.Name, request.Description, request.OrderDate);
        await _data.AddOrderAsync(order, cancellationToken);
        LogOrderCreated(_logger, order.Id, order.Name);
        return order;
    }

    /// <summary>
    /// Gets the order with the given ID, or <see langword="null"/> if not found.
    /// </summary>
    /// <param name="id">The ID of the order to retrieve.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The matching <see cref="Order"/>, or <see langword="null"/> if not found.</returns>
    public async Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        Order? result = await _data.GetOrderByIdAsync(id, cancellationToken);
        return result;
    }

    /// <summary>
    /// Gets all orders.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>An enumerable of all <see cref="Order"/> instances.</returns>
    public async Task<IEnumerable<Order>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Order> result = await _data.GetAllOrdersAsync(cancellationToken);
        return result;
    }

    /// <summary>
    /// Updates the order with the given ID using the provided request data.
    /// </summary>
    /// <param name="id">The ID of the order to update.</param>
    /// <param name="request">The updated data for the order.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The updated <see cref="Order"/>, or <see langword="null"/> if no order with the given ID exists.</returns>
    public async Task<Order?> UpdateAsync(Guid id, UpdateOrderRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        Order? existing = await _data.GetOrderByIdAsync(id, cancellationToken);
        Order? result;
        if (existing is null)
        {
            result = null;
        }
        else
        {
            Order updated = existing with { Name = request.Name, Description = request.Description };
            await _data.UpdateOrderAsync(updated, cancellationToken);
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
    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        Order? existing = await _data.GetOrderByIdAsync(id, cancellationToken);
        bool result;
        if (existing is null)
        {
            result = false;
        }
        else
        {
            await _data.DeleteOrderAsync(id, cancellationToken);
            LogOrderDeleted(_logger, id);
            result = true;
        }

        return result;
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
        Order? order = await _data.GetOrderWithDetailsAsync(id, cancellationToken);
        if (order is null)
        {
            throw new KeyNotFoundException($"Order with ID '{id}' was not found.");
        }

        await _exporter.ExportAsync(order, cancellationToken);
        LogOrderExported(_logger, id);
    }

    /// <summary>
    /// Adds a board to the order with the given ID.
    /// Has no effect if the board is already part of the order.
    /// </summary>
    /// <param name="orderId">The ID of the order.</param>
    /// <param name="boardId">The ID of the board to add.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// <see langword="true"/> if the board was added;
    /// <see langword="false"/> if the order or board was not found.
    /// </returns>
    public async Task<bool> AddBoardAsync(Guid orderId, Guid boardId, CancellationToken cancellationToken = default)
    {
        Order? order = await _data.GetOrderByIdAsync(orderId, cancellationToken);
        bool result;
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
                await _data.AddBoardToOrderAsync(orderId, boardId, cancellationToken);
                LogBoardAddedToOrder(_logger, boardId, orderId);
                result = true;
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
    public async Task<bool> RemoveBoardAsync(Guid orderId, Guid boardId, CancellationToken cancellationToken = default)
    {
        Order? order = await _data.GetOrderByIdAsync(orderId, cancellationToken);
        bool result;
        if (order is null)
        {
            result = false;
        }
        else
        {
            result = await _data.RemoveBoardFromOrderAsync(orderId, boardId, cancellationToken);
            if (result)
            {
                LogBoardRemovedFromOrder(_logger, boardId, orderId);
            }
        }

        return result;
    }

    #endregion

    #region Private Methods

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

    #endregion
}