namespace OrderManager.Core.Ports;

using OrderManager.Core.Entities;

/// <summary>
/// Port for persisting and retrieving <see cref="Order"/> entities.
/// </summary>
public interface IOrderRepository : IRepository<Order>
{
}