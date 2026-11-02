namespace OrderManager.Core.Ports;

using OrderManager.Core.Entities;

/// <summary>
/// Driven port for exporting an <see cref="Order"/> to a production line.
/// </summary>
public interface IProductionLineExporter
{
    /// <summary>
    /// Exports the given <see cref="Order"/> to the production line.
    /// </summary>
    /// <param name="order">The order to export, including its boards and components.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task ExportAsync(Order order, CancellationToken cancellationToken = default);
}