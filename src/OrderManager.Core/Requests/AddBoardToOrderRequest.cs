namespace OrderManager.Core.Requests;

/// <summary>
/// Represents the data required to add a <see cref="OrderManager.Core.Entities.Board"/> to an <see cref="OrderManager.Core.Entities.Order"/>.
/// </summary>
/// <param name="Quantity">The number of this board required for the order.</param>
public sealed record AddBoardToOrderRequest(int Quantity);