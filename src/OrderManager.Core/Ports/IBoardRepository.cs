namespace OrderManager.Core.Ports;

using OrderManager.Core.Entities;

/// <summary>
/// Port for persisting and retrieving <see cref="Board"/> entities.
/// </summary>
public interface IBoardRepository : IRepository<Board>
{
}