namespace OrderManager.Core.Entities;

/// <summary>
/// Represents the many-to-many relationship between a <see cref="Board"/> and a <see cref="Component"/>,
/// including the quantity of the component required per board.
/// </summary>
/// <param name="BoardId">The ID of the associated board.</param>
/// <param name="Board">The associated board.</param>
/// <param name="ComponentId">The ID of the associated component.</param>
/// <param name="Component">The associated component.</param>
/// <param name="QuantityPerBoard">The number of this component required per board.</param>
public sealed record BoardComponent(Guid BoardId, Board Board, Guid ComponentId, Component Component, int QuantityPerBoard);