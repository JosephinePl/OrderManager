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

    /// <summary>
    /// The service used to manage orders.
    /// </summary>
    private readonly IOrderService _orderService;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="OrdersController"/> class.
    /// </summary>
    /// <param name="orderService">The service used to manage orders.</param>
    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Returns all orders.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A list of all <see cref="Order"/> instances.</returns>
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        IEnumerable<Order> orders = await _orderService.GetAllAsync(cancellationToken);
        IActionResult result = Ok(orders);
        return result;
    }

    /// <summary>
    /// Returns the order with the given ID.
    /// </summary>
    /// <param name="id">The ID of the order to retrieve.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The matching <see cref="Order"/>, or 404 if not found.</returns>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        Order? order = await _orderService.GetByIdAsync(id, cancellationToken);
        IActionResult result = order is null ? NotFound() : Ok(order);
        return result;
    }

    /// <summary>
    /// Creates a new order.
    /// </summary>
    /// <param name="request">The data for the new order.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The newly created <see cref="Order"/> with a Location header pointing to it.</returns>
    [HttpPost]
    public async Task<IActionResult> Create(CreateOrderRequest request, CancellationToken cancellationToken)
    {
        Order order = await _orderService.CreateAsync(request, cancellationToken);
        IActionResult result = CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
        return result;
    }

    /// <summary>
    /// Updates the order with the given ID.
    /// </summary>
    /// <param name="id">The ID of the order to update.</param>
    /// <param name="request">The updated data for the order.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The updated <see cref="Order"/>, or 404 if no order with the given ID exists.</returns>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateOrderRequest request, CancellationToken cancellationToken)
    {
        Order? order = await _orderService.UpdateAsync(id, request, cancellationToken);
        IActionResult result = order is null ? NotFound() : Ok(order);
        return result;
    }

    /// <summary>
    /// Deletes the order with the given ID.
    /// </summary>
    /// <param name="id">The ID of the order to delete.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>204 No Content if deleted, or 404 if no order with the given ID exists.</returns>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        bool deleted = await _orderService.DeleteAsync(id, cancellationToken);
        IActionResult result = deleted ? NoContent() : NotFound();
        return result;
    }

    /// <summary>
    /// Adds a board to the order with the given IDs.
    /// </summary>
    /// <param name="orderId">The ID of the order.</param>
    /// <param name="boardId">The ID of the board to add.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>204 No Content if added, or 404 if the order or board was not found.</returns>
    [HttpPut("{orderId:guid}/boards/{boardId:guid}")]
    public async Task<IActionResult> AddBoard(Guid orderId, Guid boardId, CancellationToken cancellationToken)
    {
        bool added = await _orderService.AddBoardAsync(orderId, boardId, cancellationToken);
        IActionResult result = added ? NoContent() : NotFound();
        return result;
    }

    /// <summary>
    /// Removes a board from the order with the given IDs.
    /// </summary>
    /// <param name="orderId">The ID of the order.</param>
    /// <param name="boardId">The ID of the board to remove.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>204 No Content if removed, or 404 if the order or board association was not found.</returns>
    [HttpDelete("{orderId:guid}/boards/{boardId:guid}")]
    public async Task<IActionResult> RemoveBoard(Guid orderId, Guid boardId, CancellationToken cancellationToken)
    {
        bool removed = await _orderService.RemoveBoardAsync(orderId, boardId, cancellationToken);
        IActionResult result = removed ? NoContent() : NotFound();
        return result;
    }

    /// <summary>
    /// Exports the order with the given ID to the production line.
    /// </summary>
    /// <param name="id">The ID of the order to export.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>204 No Content if exported, or 404 if no order with the given ID exists.</returns>
    [HttpPost("{id:guid}/export")]
    public async Task<IActionResult> Export(Guid id, CancellationToken cancellationToken)
    {
        IActionResult result;
        try
        {
            await _orderService.ExportToProductionLineAsync(id, cancellationToken);
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