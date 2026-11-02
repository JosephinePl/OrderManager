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

    /// <summary>The component data port, used to verify component existence.</summary>
    private readonly IComponentDataPort _componentData;

    /// <summary>The logger.</summary>
    private readonly ILogger<BoardService> _logger;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="BoardService"/> class.
    /// </summary>
    /// <param name="data">The driven data port.</param>
    /// <param name="componentData">The component data port, used to verify component existence.</param>
    /// <param name="logger">The logger.</param>
    public BoardService(IBoardDataPort data, IComponentDataPort componentData, ILogger<BoardService> logger)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(componentData);
        ArgumentNullException.ThrowIfNull(logger);
        _data = data;
        _componentData = componentData;
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
    public async Task<bool> AddComponentAsync(Guid boardId, Guid componentId, int quantityPerBoard, CancellationToken cancellationToken = default)
    {
        Board? board = await _data.GetBoardByIdAsync(boardId, cancellationToken);
        bool result;
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
                await _data.AddComponentToBoardAsync(boardId, componentId, quantityPerBoard, cancellationToken);
                LogComponentAddedToBoard(_logger, componentId, boardId);
                result = true;
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
    public async Task<bool> RemoveComponentAsync(Guid boardId, Guid componentId, CancellationToken cancellationToken = default)
    {
        Board? board = await _data.GetBoardByIdAsync(boardId, cancellationToken);
        bool result;
        if (board is null)
        {
            result = false;
        }
        else
        {
            result = await _data.RemoveComponentFromBoardAsync(boardId, componentId, cancellationToken);
            if (result)
            {
                LogComponentRemovedFromBoard(_logger, componentId, boardId);
            }
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

    #endregion
}