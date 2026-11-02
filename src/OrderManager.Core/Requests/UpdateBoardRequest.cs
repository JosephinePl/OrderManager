namespace OrderManager.Core.Requests;

/// <summary>
/// Represents the data required to update an existing <see cref="OrderManager.Core.Entities.Board"/>.
/// </summary>
/// <param name="Name">The updated name of the board.</param>
/// <param name="Description">The updated description of the board.</param>
/// <param name="Length">The updated length of the board, in mm.</param>
/// <param name="Width">The updated width of the board, in mm.</param>
public sealed record UpdateBoardRequest(string Name, string Description, decimal Length, decimal Width);