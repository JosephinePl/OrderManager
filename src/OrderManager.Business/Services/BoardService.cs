namespace OrderManager.Business.Services;

using Microsoft.Extensions.Logging;
using OrderManager.Core.Entities;
using OrderManager.Core.Ports;
using OrderManager.Core.Requests;

/// <summary>
/// Application service for managing <see cref="Board"/> entities.
/// </summary>
public sealed partial class BoardService : IBoardService
{
    #region Fields

    /// <summary>The driven data port.</summary>
    private readonly IBoardDataPort _data;

    /// <summary>The logger.</summary>
    private readonly ILogger<BoardService> _logger;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="BoardService"/> class.
    /// </summary>
    /// <param name="data">The driven data port.</param>
    /// <param name="logger">The logger.</param>
    public BoardService(IBoardDataPort data, ILogger<BoardService> logger)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(logger);
        _data = data;
        _logger = logger;
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Creates a new board from the given request data.
    /// </summary>
    /// <param name="request">The data for the new board.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The newly created <see cref="Board"/>.</returns>
    public async Task<Board> CreateAsync(CreateBoardRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        Board board = new Board(Guid.NewGuid(), request.Name, request.Description, request.Length, request.Width);
        await _data.AddBoardAsync(board, cancellationToken);
        LogBoardCreated(_logger, board.Id, board.Name);
        return board;
    }

    /// <summary>
    /// Gets the board with the given ID, or <see langword="null"/> if not found.
    /// </summary>
    /// <param name="id">The ID of the board to retrieve.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The matching <see cref="Board"/>, or <see langword="null"/> if not found.</returns>
    public async Task<Board?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        Board? result = await _data.GetBoardByIdAsync(id, cancellationToken);
        return result;
    }

    /// <summary>
    /// Gets all boards.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>An enumerable of all <see cref="Board"/> instances.</returns>
    public async Task<IEnumerable<Board>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Board> result = await _data.GetAllBoardsAsync(cancellationToken);
        return result;
    }

    /// <summary>
    /// Updates the board with the given ID using the provided request data.
    /// </summary>
    /// <param name="id">The ID of the board to update.</param>
    /// <param name="request">The updated data for the board.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The updated <see cref="Board"/>, or <see langword="null"/> if no board with the given ID exists.</returns>
    public async Task<Board?> UpdateAsync(Guid id, UpdateBoardRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        Board? existing = await _data.GetBoardByIdAsync(id, cancellationToken);
        Board? result;
        if (existing is null)
        {
            result = null;
        }
        else
        {
            Board updated = existing with { Name = request.Name, Description = request.Description, Length = request.Length, Width = request.Width };
            await _data.UpdateBoardAsync(updated, cancellationToken);
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
    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        Board? existing = await _data.GetBoardByIdAsync(id, cancellationToken);
        bool result;
        if (existing is null)
        {
            result = false;
        }
        else
        {
            await _data.DeleteBoardAsync(id, cancellationToken);
            LogBoardDeleted(_logger, id);
            result = true;
        }

        return result;
    }

    #endregion

    #region Private Methods

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

    #endregion
}