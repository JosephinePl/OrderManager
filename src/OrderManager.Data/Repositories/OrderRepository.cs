namespace OrderManager.Data.Repositories;

using Microsoft.Extensions.Logging;
using OrderManager.Core.Entities;
using OrderManager.Core.Ports;

/// <summary>
/// EF Core implementation of <see cref="IOrderRepository"/>.
/// </summary>
public sealed class OrderRepository : RepositoryBase<Order>, IOrderRepository
{
    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="OrderRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="logger">The logger.</param>
    public OrderRepository(OrderManagerDbContext context, ILogger<RepositoryBase<Order>> logger)
        : base(context, logger)
    {
    }

    #endregion
}