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

    /// <summary>
    /// The service used to manage components.
    /// </summary>
    private readonly IComponentService _componentService;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="ComponentsController"/> class.
    /// </summary>
    /// <param name="componentService">The service used to manage components.</param>
    public ComponentsController(IComponentService componentService)
    {
        _componentService = componentService;
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Returns all components.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A list of all <see cref="Component"/> instances.</returns>
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        IEnumerable<Component> components = await _componentService.GetAllAsync(cancellationToken);
        IActionResult result = Ok(components);
        return result;
    }

    /// <summary>
    /// Returns the component with the given ID.
    /// </summary>
    /// <param name="id">The ID of the component to retrieve.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The matching <see cref="Component"/>, or 404 if not found.</returns>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        Component? component = await _componentService.GetByIdAsync(id, cancellationToken);
        IActionResult result = component is null ? NotFound() : Ok(component);
        return result;
    }

    /// <summary>
    /// Creates a new component.
    /// </summary>
    /// <param name="request">The data for the new component.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The newly created <see cref="Component"/> with a Location header pointing to it.</returns>
    [HttpPost]
    public async Task<IActionResult> Create(CreateComponentRequest request, CancellationToken cancellationToken)
    {
        Component component = await _componentService.CreateAsync(request, cancellationToken);
        IActionResult result = CreatedAtAction(nameof(GetById), new { id = component.Id }, component);
        return result;
    }

    /// <summary>
    /// Updates the component with the given ID.
    /// </summary>
    /// <param name="id">The ID of the component to update.</param>
    /// <param name="request">The updated data for the component.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The updated <see cref="Component"/>, or 404 if no component with the given ID exists.</returns>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateComponentRequest request, CancellationToken cancellationToken)
    {
        Component? component = await _componentService.UpdateAsync(id, request, cancellationToken);
        IActionResult result = component is null ? NotFound() : Ok(component);
        return result;
    }

    /// <summary>
    /// Deletes the component with the given ID.
    /// </summary>
    /// <param name="id">The ID of the component to delete.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>204 No Content if deleted, or 404 if no component with the given ID exists.</returns>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        bool deleted = await _componentService.DeleteAsync(id, cancellationToken);
        IActionResult result = deleted ? NoContent() : NotFound();
        return result;
    }

    #endregion
}