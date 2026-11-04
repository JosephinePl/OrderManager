namespace OrderManager.Core.Requests;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Represents the data required to update an existing <see cref="OrderManager.Core.Entities.Board"/>.
/// </summary>
/// <param name="Name">The updated name of the board.</param>
/// <param name="Description">The updated description of the board.</param>
/// <param name="Length">The updated length of the board, in mm.</param>
/// <param name="Width">The updated width of the board, in mm.</param>
public sealed record UpdateBoardRequest(
    [Required, MinLength(1)] string Name,
    string Description,
    [Range(0.001, double.MaxValue)] decimal Length,
    [Range(0.001, double.MaxValue)] decimal Width);