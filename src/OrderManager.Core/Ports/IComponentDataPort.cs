namespace OrderManager.Core.Ports;

using OrderManager.Core.Entities;

/// <summary>
/// Driven port for persistence operations on <see cref="Component"/> entities.
/// </summary>
public interface IComponentDataPort
{
    /// <summary>
    /// Gets a component by its ID, or <see langword="null"/> if not found.
    /// </summary>
    /// <param name="id">The ID of the component to retrieve.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The matching component, or <see langword="null"/> if not found.</returns>
    Task<Component?> GetComponentByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all components, optionally filtered by a case-insensitive name substring.
    /// </summary>
    /// <param name="nameFilter">When non-null and non-empty, only components whose name contains this value are returned.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A read-only list of matching components.</returns>
    Task<IReadOnlyList<Component>> GetAllComponentsAsync(string? nameFilter = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new component.
    /// </summary>
    /// <param name="component">The component to add.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task AddComponentAsync(Component component, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing component.
    /// </summary>
    /// <param name="component">The component with updated values.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task UpdateComponentAsync(Component component, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the component with the given ID.
    /// </summary>
    /// <param name="id">The ID of the component to delete.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns><see langword="true"/> if the component was deleted; <see langword="false"/> if it did not exist.</returns>
    Task<bool> DeleteComponentAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns <see langword="true"/> when the component with the given ID is associated with at least one board.
    /// </summary>
    /// <param name="componentId">The ID of the component to check.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns><see langword="true"/> if at least one board references this component; otherwise <see langword="false"/>.</returns>
    Task<bool> IsComponentAssignedToAnyBoardAsync(Guid componentId, CancellationToken cancellationToken = default);
}