namespace OrderManager.Core.Ports;

using OrderManager.Core.Entities;
using OrderManager.Core.Requests;

/// <summary>
/// Application service port for managing <see cref="Order"/> entities.
/// </summary>
public interface IOrderService
{
    /// <summary>
    /// Creates a new order from the given request data.
    /// </summary>
    /// <param name="request">The data for the new order.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The newly created <see cref="Order"/>.</returns>
    Task<Order> CreateAsync(CreateOrderRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the order with the given ID, or <see langword="null"/> if not found.
    /// </summary>
    /// <param name="id">The ID of the order to retrieve.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The matching <see cref="Order"/>, or <see langword="null"/> if not found.</returns>
    Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all orders.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>An enumerable of all <see cref="Order"/> instances.</returns>
    Task<IEnumerable<Order>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the order with the given ID using the provided request data.
    /// </summary>
    /// <param name="id">The ID of the order to update.</param>
    /// <param name="request">The updated data for the order.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The updated <see cref="Order"/>, or <see langword="null"/> if no order with the given ID exists.</returns>
    Task<Order?> UpdateAsync(Guid id, UpdateOrderRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the order with the given ID.
    /// </summary>
    /// <param name="id">The ID of the order to delete.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// <see langword="true"/> if the order was deleted;
    /// <see langword="false"/> if no order with the given ID was found.
    /// </returns>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports the order with the given ID to the production line.
    /// </summary>
    /// <param name="id">The ID of the order to export.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when no order with the given ID exists.</exception>
    Task ExportToProductionLineAsync(Guid id, CancellationToken cancellationToken = default);

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
    Task<bool> AddBoardAsync(Guid orderId, Guid boardId, CancellationToken cancellationToken = default);

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
    Task<bool> RemoveBoardAsync(Guid orderId, Guid boardId, CancellationToken cancellationToken = default);
}