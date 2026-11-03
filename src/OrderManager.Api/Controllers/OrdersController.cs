namespace OrderManager.Api.Controllers;

using Microsoft.AspNetCore.Mvc;
using OrderManager.Core.Entities;
using OrderManager.Core.Ports;
using OrderManager.Core.Requests;

/// <summary>
/// API controller for managing <see cref="Order"/> entities.
/// </summary>
[ApiController]
[Route("api/orders")]
public sealed class OrdersController : ControllerBase
{
    #region Fields

    /// <summary>The manufacturing service.</summary>
    private readonly IManufacturingService _service;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="OrdersController"/> class.
    /// </summary>
    /// <param name="service">The manufacturing service.</param>
    public OrdersController(IManufacturingService service)
    {
        ArgumentNullException.ThrowIfNull(service);
        _service = service;
    }

    #endregion

    #region Public Methods

    /// <summary>Returns all orders.</summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>An <see cref="IActionResult"/> containing all orders.</returns>
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        IEnumerable<Order> orders = await _service.GetAllOrdersAsync(cancellationToken);
        return Ok(orders);
    }

    /// <summary>Returns the order with the given ID, or 404 if not found.</summary>
    /// <param name="id">The ID of the order to retrieve.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>An <see cref="IActionResult"/> containing the order, or 404 if not found.</returns>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        Order? order = await _service.GetOrderByIdAsync(id, cancellationToken);
        return order is null ? NotFound() : Ok(order);
    }

    /// <summary>Creates a new order.</summary>
    /// <param name="request">The data for the new order.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>An <see cref="IActionResult"/> containing the created order.</returns>
    [HttpPost]
    public async Task<IActionResult> Create(CreateOrderRequest request, CancellationToken cancellationToken)
    {
        Order order = await _service.CreateOrderAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
    }

    /// <summary>Updates the order with the given ID, or 404 if not found.</summary>
    /// <param name="id">The ID of the order to update.</param>
    /// <param name="request">The updated data for the order.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>An <see cref="IActionResult"/> containing the updated order, or 404 if not found.</returns>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateOrderRequest request, CancellationToken cancellationToken)
    {
        Order? order = await _service.UpdateOrderAsync(id, request, cancellationToken);
        return order is null ? NotFound() : Ok(order);
    }

    /// <summary>Deletes the order with the given ID. Returns 204 or 404.</summary>
    /// <param name="id">The ID of the order to delete.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>An <see cref="IActionResult"/> with 204 if deleted, or 404 if not found.</returns>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        bool deleted = await _service.DeleteOrderAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }

    /// <summary>Adds a board to the order. Returns 204 or 404.</summary>
    /// <param name="orderId">The ID of the order.</param>
    /// <param name="boardId">The ID of the board to add.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>An <see cref="IActionResult"/> with 204 if added, or 404 if not found.</returns>
    [HttpPut("{orderId:guid}/boards/{boardId:guid}")]
    public async Task<IActionResult> AddBoard(Guid orderId, Guid boardId, CancellationToken cancellationToken)
    {
        bool added = await _service.AddBoardToOrderAsync(orderId, boardId, cancellationToken);
        return added ? NoContent() : NotFound();
    }

    /// <summary>Removes a board from the order. Returns 204 or 404.</summary>
    /// <param name="orderId">The ID of the order.</param>
    /// <param name="boardId">The ID of the board to remove.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>An <see cref="IActionResult"/> with 204 if removed, or 404 if not found.</returns>
    [HttpDelete("{orderId:guid}/boards/{boardId:guid}")]
    public async Task<IActionResult> RemoveBoard(Guid orderId, Guid boardId, CancellationToken cancellationToken)
    {
        bool removed = await _service.RemoveBoardFromOrderAsync(orderId, boardId, cancellationToken);
        return removed ? NoContent() : NotFound();
    }

    /// <summary>Exports the order to the production line. Returns 204 or 404.</summary>
    /// <param name="id">The ID of the order to export.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>An <see cref="IActionResult"/> with 204 if exported, or 404 if not found.</returns>
    [HttpPost("{id:guid}/export")]
    public async Task<IActionResult> Export(Guid id, CancellationToken cancellationToken)
    {
        IActionResult result;
        try
        {
            await _service.ExportToProductionLineAsync(id, cancellationToken);
            result = NoContent();
        }
        catch (KeyNotFoundException)
        {
            result = NotFound();
        }

        return result;
    }

    #endregion
}