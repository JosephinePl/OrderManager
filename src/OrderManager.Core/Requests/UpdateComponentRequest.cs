namespace OrderManager.Core.Requests;

/// <summary>
/// Represents the data required to update an existing <see cref="OrderManager.Core.Entities.Component"/>.
/// </summary>
/// <param name="Name">The updated name of the component.</param>
/// <param name="Description">The updated description of the component.</param>
/// <param name="Quantity">The updated available stock quantity of the component.</param>
public sealed record UpdateComponentRequest(string Name, string Description, int Quantity);