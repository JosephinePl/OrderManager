namespace OrderManager.Core.Ports;

using OrderManager.Core.Entities;
using OrderManager.Core.Requests;

/// <summary>
/// Application service port for managing manufacturing entities: orders, boards, and components.
/// </summary>
public interface IManufacturingService
{
    #region Methods

    /// <summary>
    /// Creates a new order from the given request data.
    /// </summary>
    /// <param name="request">The data for the new order.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The newly created <see cref="Order"/>.</returns>
    Task<Order> CreateOrderAsync(CreateOrderRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the order with the given ID, or <see langword="null"/> if not found.
    /// </summary>
    /// <param name="id">The ID of the order to retrieve.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The matching <see cref="Order"/>, or <see langword="null"/> if not found.</returns>
    Task<Order?> GetOrderByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all orders, optionally filtered by a case-insensitive name substring.
    /// </summary>
    /// <param name="nameFilter">When non-null and non-empty, only orders whose name contains this value are returned.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>An enumerable of matching <see cref="Order"/> instances.</returns>
    Task<IEnumerable<Order>> GetAllOrdersAsync(string? nameFilter = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the order with the given ID using the provided request data.
    /// </summary>
    /// <param name="id">The ID of the order to update.</param>
    /// <param name="request">The updated data for the order.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The updated <see cref="Order"/>, or <see langword="null"/> if no order with the given ID exists.</returns>
    Task<Order?> UpdateOrderAsync(Guid id, UpdateOrderRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the order with the given ID.
    /// </summary>
    /// <param name="id">The ID of the order to delete.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// <see langword="true"/> if the order was deleted;
    /// <see langword="false"/> if no order with the given ID was found.
    /// </returns>
    Task<bool> DeleteOrderAsync(Guid id, CancellationToken cancellationToken = default);

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
    /// <param name="quantity">The number of this board required for the order.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// <see langword="true"/> if the board was added or updated;
    /// <see langword="false"/> if the order or board was not found.
    /// </returns>
    Task<bool> AddBoardToOrderAsync(Guid orderId, Guid boardId, int quantity, CancellationToken cancellationToken = default);

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
    Task<bool> RemoveBoardFromOrderAsync(Guid orderId, Guid boardId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new board from the given request data.
    /// </summary>
    /// <param name="request">The data for the new board.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The newly created <see cref="Board"/>.</returns>
    Task<Board> CreateBoardAsync(CreateBoardRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the board with the given ID, or <see langword="null"/> if not found.
    /// </summary>
    /// <param name="id">The ID of the board to retrieve.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The matching <see cref="Board"/>, or <see langword="null"/> if not found.</returns>
    Task<Board?> GetBoardByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all boards, optionally filtered by a case-insensitive name substring.
    /// </summary>
    /// <param name="nameFilter">When non-null and non-empty, only boards whose name contains this value are returned.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>An enumerable of matching <see cref="Board"/> instances.</returns>
    Task<IEnumerable<Board>> GetAllBoardsAsync(string? nameFilter = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the board with the given ID using the provided request data.
    /// </summary>
    /// <param name="id">The ID of the board to update.</param>
    /// <param name="request">The updated data for the board.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The updated <see cref="Board"/>, or <see langword="null"/> if no board with the given ID exists.</returns>
    Task<Board?> UpdateBoardAsync(Guid id, UpdateBoardRequest request, CancellationToken cancellationToken = default);

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
    Task<bool> DeleteBoardAsync(Guid id, CancellationToken cancellationToken = default);

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
    Task<bool> AddComponentToBoardAsync(Guid boardId, Guid componentId, int quantityPerBoard, CancellationToken cancellationToken = default);

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
    Task<bool> RemoveComponentFromBoardAsync(Guid boardId, Guid componentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new component from the given request data.
    /// </summary>
    /// <param name="request">The data for the new component.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The newly created <see cref="Component"/>.</returns>
    Task<Component> CreateComponentAsync(CreateComponentRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the component with the given ID, or <see langword="null"/> if not found.
    /// </summary>
    /// <param name="id">The ID of the component to retrieve.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The matching <see cref="Component"/>, or <see langword="null"/> if not found.</returns>
    Task<Component?> GetComponentByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all components, optionally filtered by a case-insensitive name substring.
    /// </summary>
    /// <param name="nameFilter">When non-null and non-empty, only components whose name contains this value are returned.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>An enumerable of matching <see cref="Component"/> instances.</returns>
    Task<IEnumerable<Component>> GetAllComponentsAsync(string? nameFilter = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the component with the given ID using the provided request data.
    /// </summary>
    /// <param name="id">The ID of the component to update.</param>
    /// <param name="request">The updated data for the component.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The updated <see cref="Component"/>, or <see langword="null"/> if no component with the given ID exists.</returns>
    Task<Component?> UpdateComponentAsync(Guid id, UpdateComponentRequest request, CancellationToken cancellationToken = default);

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
    Task<bool> DeleteComponentAsync(Guid id, CancellationToken cancellationToken = default);

    #endregion
}