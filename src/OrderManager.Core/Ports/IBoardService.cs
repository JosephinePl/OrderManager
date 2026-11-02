namespace OrderManager.Core.Ports;

using OrderManager.Core.Entities;
using OrderManager.Core.Requests;

/// <summary>
/// Application service port for managing <see cref="Board"/> entities.
/// </summary>
public interface IBoardService
{
    /// <summary>
    /// Creates a new board from the given request data.
    /// </summary>
    /// <param name="request">The data for the new board.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The newly created <see cref="Board"/>.</returns>
    Task<Board> CreateAsync(CreateBoardRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the board with the given ID, or <see langword="null"/> if not found.
    /// </summary>
    /// <param name="id">The ID of the board to retrieve.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The matching <see cref="Board"/>, or <see langword="null"/> if not found.</returns>
    Task<Board?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all boards.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>An enumerable of all <see cref="Board"/> instances.</returns>
    Task<IEnumerable<Board>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the board with the given ID using the provided request data.
    /// </summary>
    /// <param name="id">The ID of the board to update.</param>
    /// <param name="request">The updated data for the board.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The updated <see cref="Board"/>, or <see langword="null"/> if no board with the given ID exists.</returns>
    Task<Board?> UpdateAsync(Guid id, UpdateBoardRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the board with the given ID.
    /// </summary>
    /// <param name="id">The ID of the board to delete.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// <see langword="true"/> if the board was deleted;
    /// <see langword="false"/> if no board with the given ID was found.
    /// </returns>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a component to the board with the given ID, or updates its quantity if it is already present.
    /// </summary>
    /// <param name="boardId">The ID of the board.</param>
    /// <param name="componentId">The ID of the component to add.</param>
    /// <param name="quantityPerBoard">The number of this component required per board.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// <see langword="true"/> if the component was added or updated;
    /// <see langword="false"/> if the board or component was not found.
    /// </returns>
    Task<bool> AddComponentAsync(Guid boardId, Guid componentId, int quantityPerBoard, CancellationToken cancellationToken = default);

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
    Task<bool> RemoveComponentAsync(Guid boardId, Guid componentId, CancellationToken cancellationToken = default);
}