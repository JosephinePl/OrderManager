namespace OrderManager.Data.Adapters;

using System.Text.Json;
using Microsoft.Extensions.Logging;
using OrderManager.Core.Entities;
using OrderManager.Core.Ports;

/// <summary>
/// Simulates exporting an <see cref="Order"/> to a production line by writing a JSON file to disk.
/// </summary>
public sealed class SimulatedFileExportAdapter : IProductionLineExporter
{
    #region Constants

    /// <summary>The directory to which export files are written.</summary>
    private static readonly string ExportDirectory = Path.Combine(Path.GetTempPath(), "OrderManagerExports");

    /// <summary>Cached serializer options shared across all export calls.</summary>
    private static readonly JsonSerializerOptions SerializerOptions = new JsonSerializerOptions { WriteIndented = true };

    #endregion

    #region Fields

    /// <summary>The logger.</summary>
    private readonly ILogger<SimulatedFileExportAdapter> _logger;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="SimulatedFileExportAdapter"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public SimulatedFileExportAdapter(ILogger<SimulatedFileExportAdapter> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Exports the given <see cref="Order"/> to the production line by serializing it to a JSON file.
    /// </summary>
    /// <param name="order">The order to export, including its boards and components.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task ExportAsync(Order order, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(order);
        Directory.CreateDirectory(ExportDirectory);

        ExportPayload payload = BuildPayload(order);

        string json = JsonSerializer.Serialize(payload, SerializerOptions);

        string fileName = $"order_{order.Id:N}_{DateTimeOffset.UtcNow:yyyyMMdd_HHmmss}.json";
        string filePath = Path.Combine(ExportDirectory, fileName);
        await File.WriteAllTextAsync(filePath, json, cancellationToken);

        // filePath is pre-computed for the file write above — CA1873 does not apply here.
#pragma warning disable CA1873
        _logger.LogInformation("Simulated export of order {OrderId} written to {FilePath}.", order.Id, filePath);
#pragma warning restore CA1873
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Projects an <see cref="Order"/> into a serialization-safe payload without circular references.
    /// </summary>
    /// <param name="order">The order to project.</param>
    /// <returns>The export payload.</returns>
    private static ExportPayload BuildPayload(Order order)
    {
        IEnumerable<ExportBoardPayload> boards = order.OrderBoards.Select(ob => BuildBoardPayload(ob.Board));
        ExportPayload result = new ExportPayload(order.Id, order.Name, order.Description, order.OrderDate, boards);
        return result;
    }

    /// <summary>
    /// Projects a <see cref="Board"/> into a serialization-safe board payload.
    /// </summary>
    /// <param name="board">The board to project.</param>
    /// <returns>The board payload.</returns>
    private static ExportBoardPayload BuildBoardPayload(Board board)
    {
        IEnumerable<ExportComponentPayload> components = board.BoardComponents.Select(
            bc => new ExportComponentPayload(bc.Component.Id, bc.Component.Name, bc.Component.Description, bc.Component.Quantity, bc.QuantityPerBoard));
        ExportBoardPayload result = new ExportBoardPayload(board.Id, board.Name, board.Description, board.Length, board.Width, components);
        return result;
    }

    #endregion

    #region Classes

    /// <summary>Serialization payload for an order export.</summary>
    /// <param name="Id">The order ID.</param>
    /// <param name="Name">The order name.</param>
    /// <param name="Description">The order description.</param>
    /// <param name="OrderDate">The order date.</param>
    /// <param name="Boards">The boards included in the order.</param>
    private sealed record ExportPayload(Guid Id, string Name, string Description, DateTimeOffset OrderDate, IEnumerable<ExportBoardPayload> Boards);

    /// <summary>Serialization payload for a board within an order export.</summary>
    /// <param name="Id">The board ID.</param>
    /// <param name="Name">The board name.</param>
    /// <param name="Description">The board description.</param>
    /// <param name="Length">The board length in mm.</param>
    /// <param name="Width">The board width in mm.</param>
    /// <param name="Components">The components placed on the board.</param>
    private sealed record ExportBoardPayload(Guid Id, string Name, string Description, decimal Length, decimal Width, IEnumerable<ExportComponentPayload> Components);

    /// <summary>Serialization payload for a component within a board export.</summary>
    /// <param name="Id">The component ID.</param>
    /// <param name="Name">The component name.</param>
    /// <param name="Description">The component description.</param>
    /// <param name="Quantity">The available stock quantity.</param>
    /// <param name="QuantityPerBoard">The number of this component required per board.</param>
    private sealed record ExportComponentPayload(Guid Id, string Name, string Description, int Quantity, int QuantityPerBoard);

    #endregion
}