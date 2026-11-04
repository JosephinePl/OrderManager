namespace OrderManager.Core.Requests;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Represents the data required to add a <see cref="OrderManager.Core.Entities.Board"/> to an <see cref="OrderManager.Core.Entities.Order"/>.
/// </summary>
/// <param name="Quantity">The number of this board required for the order.</param>
public sealed record AddBoardToOrderRequest([Range(1, int.MaxValue)] int Quantity);