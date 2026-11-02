namespace OrderManager.Core.Requests;

/// <summary>
/// Represents the data required to add a <see cref="OrderManager.Core.Entities.Component"/> to a <see cref="OrderManager.Core.Entities.Board"/>.
/// </summary>
/// <param name="QuantityPerBoard">The number of this component required per board.</param>
public sealed record AddComponentToBoardRequest(int QuantityPerBoard);