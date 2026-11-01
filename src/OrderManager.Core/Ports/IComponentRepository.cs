namespace OrderManager.Core.Ports;

using OrderManager.Core.Entities;

/// <summary>
/// Port for persisting and retrieving <see cref="Component"/> entities.
/// </summary>
public interface IComponentRepository : IRepository<Component>
{
}