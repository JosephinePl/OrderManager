namespace OrderManager.Data.Repositories;

using Microsoft.Extensions.Logging;
using OrderManager.Core.Entities;
using OrderManager.Core.Ports;

/// <summary>
/// EF Core implementation of <see cref="IBoardRepository"/>.
/// </summary>
public sealed class BoardRepository : RepositoryBase<Board>, IBoardRepository
{
    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="BoardRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="logger">The logger.</param>
    public BoardRepository(OrderManagerDbContext context, ILogger<RepositoryBase<Board>> logger)
        : base(context, logger)
    {
    }

    #endregion
}