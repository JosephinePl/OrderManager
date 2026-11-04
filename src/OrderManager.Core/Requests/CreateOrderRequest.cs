namespace OrderManager.Core.Requests;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Represents the data required to create a new <see cref="OrderManager.Core.Entities.Order"/>.
/// </summary>
/// <param name="Name">The name of the order.</param>
/// <param name="Description">A description of the order.</param>
/// <param name="OrderDate">The timestamp at which the order was placed.</param>
public sealed record CreateOrderRequest(
    [Required, MinLength(1)] string Name,
    string Description,
    DateTimeOffset OrderDate);