namespace OrderManager.Data.Repositories;

using Microsoft.Extensions.Logging;
using OrderManager.Core.Entities;
using OrderManager.Core.Ports;

/// <summary>
/// EF Core implementation of <see cref="IComponentRepository"/>.
/// </summary>
public sealed class ComponentRepository : RepositoryBase<Component>, IComponentRepository
{
    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="ComponentRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="logger">The logger.</param>
    public ComponentRepository(OrderManagerDbContext context, ILogger<RepositoryBase<Component>> logger)
        : base(context, logger)
    {
    }

    #endregion
}