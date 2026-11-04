namespace OrderManager.Core.Requests;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Represents the data required to update an existing <see cref="OrderManager.Core.Entities.Order"/>.
/// </summary>
/// <param name="Name">The updated name of the order.</param>
/// <param name="Description">The updated description of the order.</param>
public sealed record UpdateOrderRequest(
    [Required, MinLength(1)] string Name,
    string Description);