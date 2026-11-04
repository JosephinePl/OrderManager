namespace OrderManager.Core.Requests;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Represents the data required to create a new <see cref="OrderManager.Core.Entities.Board"/>.
/// </summary>
/// <param name="Name">The name of the board.</param>
/// <param name="Description">A description of the board.</param>
/// <param name="Length">The length of the board, in mm.</param>
/// <param name="Width">The width of the board, in mm.</param>
public sealed record CreateBoardRequest(
    [Required, MinLength(1)] string Name,
    string Description,
    [Range(0.001, double.MaxValue)] decimal Length,
    [Range(0.001, double.MaxValue)] decimal Width);