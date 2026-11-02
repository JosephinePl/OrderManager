namespace OrderManager.Core.Ports;

using OrderManager.Core.Entities;
using OrderManager.Core.Requests;

/// <summary>
/// Application service port for managing <see cref="Component"/> entities.
/// </summary>
public interface IComponentService
{
    /// <summary>
    /// Creates a new component from the given request data.
    /// </summary>
    /// <param name="request">The data for the new component.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The newly created <see cref="Component"/>.</returns>
    Task<Component> CreateAsync(CreateComponentRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the component with the given ID, or <see langword="null"/> if not found.
    /// </summary>
    /// <param name="id">The ID of the component to retrieve.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The matching <see cref="Component"/>, or <see langword="null"/> if not found.</returns>
    Task<Component?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all components.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>An enumerable of all <see cref="Component"/> instances.</returns>
    Task<IEnumerable<Component>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the component with the given ID using the provided request data.
    /// </summary>
    /// <param name="id">The ID of the component to update.</param>
    /// <param name="request">The updated data for the component.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The updated <see cref="Component"/>, or <see langword="null"/> if no component with the given ID exists.</returns>
    Task<Component?> UpdateAsync(Guid id, UpdateComponentRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the component with the given ID.
    /// </summary>
    /// <param name="id">The ID of the component to delete.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// <see langword="true"/> if the component was deleted;
    /// <see langword="false"/> if no component with the given ID was found.
    /// </returns>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}