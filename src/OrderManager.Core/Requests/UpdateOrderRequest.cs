namespace OrderManager.Core.Requests;

/// <summary>
/// Represents the data required to update an existing <see cref="OrderManager.Core.Entities.Order"/>.
/// </summary>
/// <param name="Name">The updated name of the order.</param>
/// <param name="Description">The updated description of the order.</param>
public sealed record UpdateOrderRequest(string Name, string Description);