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

    /// <summary>The manufacturing service.</summary>
    private readonly IManufacturingService _service;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="BoardsController"/> class.
    /// </summary>
    /// <param name="service">The manufacturing service.</param>
    public BoardsController(IManufacturingService service)
    {
        ArgumentNullException.ThrowIfNull(service);
        _service = service;
    }

    #endregion

    #region Public Methods

    /// <summary>Returns all boards, optionally filtered by name substring.</summary>
    /// <param name="name">When provided, only boards whose name contains this value (case-insensitive) are returned.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>An <see cref="IActionResult"/> containing matching boards.</returns>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? name, CancellationToken cancellationToken)
    {
        IEnumerable<Board> boards = await _service.GetAllBoardsAsync(name, cancellationToken);
        return Ok(boards);
    }

    /// <summary>Returns the board with the given ID, or 404 if not found.</summary>
    /// <param name="id">The ID of the board to retrieve.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>An <see cref="IActionResult"/> containing the board, or 404 if not found.</returns>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        Board? board = await _service.GetBoardByIdAsync(id, cancellationToken);
        return board is null ? NotFound() : Ok(board);
    }

    /// <summary>Creates a new board.</summary>
    /// <param name="request">The data for the new board.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>An <see cref="IActionResult"/> containing the created board.</returns>
    [HttpPost]
    public async Task<IActionResult> Create(CreateBoardRequest request, CancellationToken cancellationToken)
    {
        Board board = await _service.CreateBoardAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = board.Id }, board);
    }

    /// <summary>Updates the board with the given ID, or 404 if not found.</summary>
    /// <param name="id">The ID of the board to update.</param>
    /// <param name="request">The updated data for the board.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>An <see cref="IActionResult"/> containing the updated board, or 404 if not found.</returns>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateBoardRequest request, CancellationToken cancellationToken)
    {
        Board? board = await _service.UpdateBoardAsync(id, request, cancellationToken);
        return board is null ? NotFound() : Ok(board);
    }

    /// <summary>Adds a component to the board, or updates its quantity. Returns 204 or 404.</summary>
    /// <param name="boardId">The ID of the board.</param>
    /// <param name="componentId">The ID of the component to add.</param>
    /// <param name="request">The quantity data for the component.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>An <see cref="IActionResult"/> with 204 if added or updated, or 404 if not found.</returns>
    [HttpPut("{boardId:guid}/components/{componentId:guid}")]
    public async Task<IActionResult> AddComponent(Guid boardId, Guid componentId, AddComponentToBoardRequest request, CancellationToken cancellationToken)
    {
        bool added = await _service.AddComponentToBoardAsync(boardId, componentId, request.QuantityPerBoard, cancellationToken);
        return added ? NoContent() : NotFound();
    }

    /// <summary>Removes a component from the board. Returns 204 or 404.</summary>
    /// <param name="boardId">The ID of the board.</param>
    /// <param name="componentId">The ID of the component to remove.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>An <see cref="IActionResult"/> with 204 if removed, or 404 if not found.</returns>
    [HttpDelete("{boardId:guid}/components/{componentId:guid}")]
    public async Task<IActionResult> RemoveComponent(Guid boardId, Guid componentId, CancellationToken cancellationToken)
    {
        bool removed = await _service.RemoveComponentFromBoardAsync(boardId, componentId, cancellationToken);
        return removed ? NoContent() : NotFound();
    }

    /// <summary>Deletes the board with the given ID. Returns 204 or 404.</summary>
    /// <param name="id">The ID of the board to delete.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>An <see cref="IActionResult"/> with 204 if deleted, or 404 if not found.</returns>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        bool deleted = await _service.DeleteBoardAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }

    #endregion
}