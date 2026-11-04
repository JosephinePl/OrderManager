namespace OrderManager.Core.Ports;

using OrderManager.Core.Entities;

/// <summary>
/// Driven port for persistence operations on <see cref="Order"/> entities.
/// </summary>
public interface IOrderDataPort
{
    /// <summary>
    /// Gets an order by its ID with all related boards and components eagerly loaded,
    /// or <see langword="null"/> if not found.
    /// </summary>
    /// <param name="id">The ID of the order to retrieve.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The matching order with boards and components populated, or <see langword="null"/> if not found.</returns>
    Task<Order?> GetOrderWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an order by its ID, or <see langword="null"/> if not found.
    /// </summary>
    /// <param name="id">The ID of the order to retrieve.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The matching order, or <see langword="null"/> if not found.</returns>
    Task<Order?> GetOrderByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all orders.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A read-only list of all orders.</returns>
    Task<IReadOnlyList<Order>> GetAllOrdersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new order.
    /// </summary>
    /// <param name="order">The order to add.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task AddOrderAsync(Order order, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing order.
    /// </summary>
    /// <param name="order">The order with updated values.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task UpdateOrderAsync(Order order, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the order with the given ID.
    /// </summary>
    /// <param name="id">The ID of the order to delete.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns><see langword="true"/> if the order was deleted; <see langword="false"/> if it did not exist.</returns>
    Task<bool> DeleteOrderAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a board association to the order with the given ID, or updates the quantity if it already exists.
    /// </summary>
    /// <param name="orderId">The ID of the order.</param>
    /// <param name="boardId">The ID of the board to associate.</param>
    /// <param name="quantity">The number of this board required for the order.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task AddBoardToOrderAsync(Guid orderId, Guid boardId, int quantity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes the board association from the order with the given ID.
    /// </summary>
    /// <param name="orderId">The ID of the order.</param>
    /// <param name="boardId">The ID of the board to disassociate.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns><see langword="true"/> if the association was removed; <see langword="false"/> if it did not exist.</returns>
    Task<bool> RemoveBoardFromOrderAsync(Guid orderId, Guid boardId, CancellationToken cancellationToken = default);
}