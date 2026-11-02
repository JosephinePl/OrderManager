namespace OrderManager.Business.Services;

using Microsoft.Extensions.Logging;
using OrderManager.Core.Entities;
using OrderManager.Core.Ports;
using OrderManager.Core.Requests;

/// <summary>
/// Application service for managing <see cref="Component"/> entities.
/// </summary>
public sealed partial class ComponentService : IComponentService
{
    #region Fields

    /// <summary>The driven data port.</summary>
    private readonly IComponentDataPort _data;

    /// <summary>The logger.</summary>
    private readonly ILogger<ComponentService> _logger;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="ComponentService"/> class.
    /// </summary>
    /// <param name="data">The driven data port.</param>
    /// <param name="logger">The logger.</param>
    public ComponentService(IComponentDataPort data, ILogger<ComponentService> logger)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(logger);
        _data = data;
        _logger = logger;
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Creates a new component from the given request data.
    /// </summary>
    /// <param name="request">The data for the new component.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The newly created <see cref="Component"/>.</returns>
    public async Task<Component> CreateAsync(CreateComponentRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        Component component = new Component(Guid.NewGuid(), request.Name, request.Description, request.Quantity);
        await _data.AddComponentAsync(component, cancellationToken);
        LogComponentCreated(_logger, component.Id, component.Name);
        return component;
    }

    /// <summary>
    /// Gets the component with the given ID, or <see langword="null"/> if not found.
    /// </summary>
    /// <param name="id">The ID of the component to retrieve.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The matching <see cref="Component"/>, or <see langword="null"/> if not found.</returns>
    public async Task<Component?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        Component? result = await _data.GetComponentByIdAsync(id, cancellationToken);
        return result;
    }

    /// <summary>
    /// Gets all components.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>An enumerable of all <see cref="Component"/> instances.</returns>
    public async Task<IEnumerable<Component>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Component> result = await _data.GetAllComponentsAsync(cancellationToken);
        return result;
    }

    /// <summary>
    /// Updates the component with the given ID using the provided request data.
    /// </summary>
    /// <param name="id">The ID of the component to update.</param>
    /// <param name="request">The updated data for the component.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The updated <see cref="Component"/>, or <see langword="null"/> if no component with the given ID exists.</returns>
    public async Task<Component?> UpdateAsync(Guid id, UpdateComponentRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        Component? existing = await _data.GetComponentByIdAsync(id, cancellationToken);
        Component? result;
        if (existing is null)
        {
            result = null;
        }
        else
        {
            Component updated = existing with { Name = request.Name, Description = request.Description, Quantity = request.Quantity };
            await _data.UpdateComponentAsync(updated, cancellationToken);
            LogComponentUpdated(_logger, id);
            result = updated;
        }

        return result;
    }

    /// <summary>
    /// Deletes the component with the given ID.
    /// </summary>
    /// <param name="id">The ID of the component to delete.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// <see langword="true"/> if the component was deleted;
    /// <see langword="false"/> if no component with the given ID was found.
    /// </returns>
    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        Component? existing = await _data.GetComponentByIdAsync(id, cancellationToken);
        bool result;
        if (existing is null)
        {
            result = false;
        }
        else
        {
            await _data.DeleteComponentAsync(id, cancellationToken);
            LogComponentDeleted(_logger, id);
            result = true;
        }

        return result;
    }

    #endregion

    #region Private Methods

    /// <summary>Logs the creation of a component.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="componentId">The ID of the created component.</param>
    /// <param name="componentName">The name of the created component.</param>
    [LoggerMessage(Level = LogLevel.Information, Message = "Created component {ComponentId} ({ComponentName}).")]
    private static partial void LogComponentCreated(ILogger logger, Guid componentId, string componentName);

    /// <summary>Logs the update of a component.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="componentId">The ID of the updated component.</param>
    [LoggerMessage(Level = LogLevel.Information, Message = "Updated component {ComponentId}.")]
    private static partial void LogComponentUpdated(ILogger logger, Guid componentId);

    /// <summary>Logs the deletion of a component.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="componentId">The ID of the deleted component.</param>
    [LoggerMessage(Level = LogLevel.Information, Message = "Deleted component {ComponentId}.")]
    private static partial void LogComponentDeleted(ILogger logger, Guid componentId);

    #endregion
}