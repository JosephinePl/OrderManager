namespace OrderManager.Core.Ports;

using OrderManager.Core.Entities;

/// <summary>
/// Driven port for persistence operations on <see cref="Board"/> entities.
/// </summary>
public interface IBoardDataPort
{
    /// <summary>
    /// Gets a board by its ID, or <see langword="null"/> if not found.
    /// </summary>
    /// <param name="id">The ID of the board to retrieve.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The matching board, or <see langword="null"/> if not found.</returns>
    Task<Board?> GetBoardByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all boards.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A read-only list of all boards.</returns>
    Task<IReadOnlyList<Board>> GetAllBoardsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new board.
    /// </summary>
    /// <param name="board">The board to add.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task AddBoardAsync(Board board, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing board.
    /// </summary>
    /// <param name="board">The board with updated values.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task UpdateBoardAsync(Board board, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the board with the given ID.
    /// </summary>
    /// <param name="id">The ID of the board to delete.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task DeleteBoardAsync(Guid id, CancellationToken cancellationToken = default);
}