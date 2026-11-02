namespace OrderManager.Api.Controllers;

using Microsoft.AspNetCore.Mvc;
using OrderManager.Core.Entities;
using OrderManager.Core.Ports;
using OrderManager.Core.Requests;

/// <summary>
/// API controller for managing <see cref="Board"/> entities.
/// </summary>
[ApiController]
[Route("api/boards")]
public sealed class BoardsController : ControllerBase
{
    #region Fields

    /// <summary>
    /// The service used to manage boards.
    /// </summary>
    private readonly IBoardService _boardService;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="BoardsController"/> class.
    /// </summary>
    /// <param name="boardService">The service used to manage boards.</param>
    public BoardsController(IBoardService boardService)
    {
        _boardService = boardService;
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Returns all boards.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A list of all <see cref="Board"/> instances.</returns>
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        IEnumerable<Board> boards = await _boardService.GetAllAsync(cancellationToken);
        IActionResult result = Ok(boards);
        return result;
    }

    /// <summary>
    /// Returns the board with the given ID.
    /// </summary>
    /// <param name="id">The ID of the board to retrieve.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The matching <see cref="Board"/>, or 404 if not found.</returns>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        Board? board = await _boardService.GetByIdAsync(id, cancellationToken);
        IActionResult result = board is null ? NotFound() : Ok(board);
        return result;
    }

    /// <summary>
    /// Creates a new board.
    /// </summary>
    /// <param name="request">The data for the new board.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The newly created <see cref="Board"/> with a Location header pointing to it.</returns>
    [HttpPost]
    public async Task<IActionResult> Create(CreateBoardRequest request, CancellationToken cancellationToken)
    {
        Board board = await _boardService.CreateAsync(request, cancellationToken);
        IActionResult result = CreatedAtAction(nameof(GetById), new { id = board.Id }, board);
        return result;
    }

    /// <summary>
    /// Updates the board with the given ID.
    /// </summary>
    /// <param name="id">The ID of the board to update.</param>
    /// <param name="request">The updated data for the board.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The updated <see cref="Board"/>, or 404 if no board with the given ID exists.</returns>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateBoardRequest request, CancellationToken cancellationToken)
    {
        Board? board = await _boardService.UpdateAsync(id, request, cancellationToken);
        IActionResult result = board is null ? NotFound() : Ok(board);
        return result;
    }

    /// <summary>
    /// Adds a component to the board with the given IDs, or updates the quantity if already present.
    /// </summary>
    /// <param name="boardId">The ID of the board.</param>
    /// <param name="componentId">The ID of the component to add.</param>
    /// <param name="request">The quantity of the component required per board.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>204 No Content if added or updated, or 404 if the board or component was not found.</returns>
    [HttpPut("{boardId:guid}/components/{componentId:guid}")]
    public async Task<IActionResult> AddComponent(Guid boardId, Guid componentId, AddComponentToBoardRequest request, CancellationToken cancellationToken)
    {
        bool added = await _boardService.AddComponentAsync(boardId, componentId, request.QuantityPerBoard, cancellationToken);
        IActionResult result = added ? NoContent() : NotFound();
        return result;
    }

    /// <summary>
    /// Removes a component from the board with the given IDs.
    /// </summary>
    /// <param name="boardId">The ID of the board.</param>
    /// <param name="componentId">The ID of the component to remove.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>204 No Content if removed, or 404 if the board or component association was not found.</returns>
    [HttpDelete("{boardId:guid}/components/{componentId:guid}")]
    public async Task<IActionResult> RemoveComponent(Guid boardId, Guid componentId, CancellationToken cancellationToken)
    {
        bool removed = await _boardService.RemoveComponentAsync(boardId, componentId, cancellationToken);
        IActionResult result = removed ? NoContent() : NotFound();
        return result;
    }

    /// <summary>
    /// Deletes the board with the given ID.
    /// </summary>
    /// <param name="id">The ID of the board to delete.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>204 No Content if deleted, or 404 if no board with the given ID exists.</returns>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        bool deleted = await _boardService.DeleteAsync(id, cancellationToken);
        IActionResult result = deleted ? NoContent() : NotFound();
        return result;
    }

    #endregion
}