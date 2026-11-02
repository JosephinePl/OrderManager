namespace OrderManager.Core.Requests;

/// <summary>
/// Represents the data required to create a new <see cref="OrderManager.Core.Entities.Component"/>.
/// </summary>
/// <param name="Name">The name of the component.</param>
/// <param name="Description">A description of the component.</param>
/// <param name="Quantity">The available stock quantity of the component.</param>
public sealed record CreateComponentRequest(string Name, string Description, int Quantity);