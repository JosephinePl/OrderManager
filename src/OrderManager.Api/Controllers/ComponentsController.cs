namespace OrderManager.Api.Controllers;

using Microsoft.AspNetCore.Mvc;
using OrderManager.Core.Entities;
using OrderManager.Core.Ports;
using OrderManager.Core.Requests;

/// <summary>
/// API controller for managing <see cref="Component"/> entities.
/// </summary>
[ApiController]
[Route("api/components")]
public sealed class ComponentsController : ControllerBase
{
    #region Fields

    /// <summary>The manufacturing service.</summary>
    private readonly IManufacturingService _service;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="ComponentsController"/> class.
    /// </summary>
    /// <param name="service">The manufacturing service.</param>
    public ComponentsController(IManufacturingService service)
    {
        ArgumentNullException.ThrowIfNull(service);
        _service = service;
    }

    #endregion

    #region Public Methods

    /// <summary>Returns all components, optionally filtered by name substring.</summary>
    /// <param name="name">When provided, only components whose name contains this value (case-insensitive) are returned.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>An <see cref="IActionResult"/> containing matching components.</returns>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? name, CancellationToken cancellationToken)
    {
        IEnumerable<Component> components = await _service.GetAllComponentsAsync(name, cancellationToken);
        return Ok(components);
    }

    /// <summary>Returns the component with the given ID, or 404 if not found.</summary>
    /// <param name="id">The ID of the component to retrieve.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>An <see cref="IActionResult"/> containing the component, or 404 if not found.</returns>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        Component? component = await _service.GetComponentByIdAsync(id, cancellationToken);
        return component is null ? NotFound() : Ok(component);
    }

    /// <summary>Creates a new component.</summary>
    /// <param name="request">The data for the new component.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>An <see cref="IActionResult"/> containing the created component.</returns>
    [HttpPost]
    public async Task<IActionResult> Create(CreateComponentRequest request, CancellationToken cancellationToken)
    {
        Component component = await _service.CreateComponentAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = component.Id }, component);
    }

    /// <summary>Updates the component with the given ID, or 404 if not found.</summary>
    /// <param name="id">The ID of the component to update.</param>
    /// <param name="request">The updated data for the component.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>An <see cref="IActionResult"/> containing the updated component, or 404 if not found.</returns>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateComponentRequest request, CancellationToken cancellationToken)
    {
        Component? component = await _service.UpdateComponentAsync(id, request, cancellationToken);
        return component is null ? NotFound() : Ok(component);
    }

    /// <summary>Deletes the component with the given ID. Returns 204 or 404.</summary>
    /// <param name="id">The ID of the component to delete.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>An <see cref="IActionResult"/> with 204 if deleted, or 404 if not found.</returns>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        bool deleted = await _service.DeleteComponentAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }

    #endregion
}