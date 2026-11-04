namespace OrderManager.Data.Adapters;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OrderManager.Core.Entities;
using OrderManager.Core.Exceptions;
using OrderManager.Core.Ports;

/// <summary>
/// EF Core implementation of <see cref="IOrderDataPort"/>, <see cref="IBoardDataPort"/>, and <see cref="IComponentDataPort"/>.
/// </summary>
public sealed class EfCoreDataAdapter : IOrderDataPort, IBoardDataPort, IComponentDataPort
{
    #region Fields

    /// <summary>The database context.</summary>
    private readonly OrderManagerDbContext _context;

    /// <summary>The logger.</summary>
    private readonly ILogger<EfCoreDataAdapter> _logger;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="EfCoreDataAdapter"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="logger">The logger.</param>
    public EfCoreDataAdapter(OrderManagerDbContext context, ILogger<EfCoreDataAdapter> logger)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(logger);
        _context = context;
        _logger = logger;
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Gets an order by its ID with all related boards and components eagerly loaded,
    /// or <see langword="null"/> if not found.
    /// </summary>
    /// <param name="id">The ID of the order to retrieve.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The matching order with boards and components populated, or <see langword="null"/> if not found.</returns>
    public async Task<Order?> GetOrderWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        Order? order = await _context.Orders.FindAsync(new object[] { id }, cancellationToken);
        if (order is not null)
        {
            await _context.Entry(order)
                .Collection(o => o.OrderBoards)
                .Query()
                .Include(ob => ob.Board)
                .LoadAsync(cancellationToken);
        }

        return order;
    }

    /// <summary>
    /// Gets an order by its ID, or <see langword="null"/> if not found.
    /// </summary>
    /// <param name="id">The ID of the order to retrieve.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The matching order, or <see langword="null"/> if not found.</returns>
    public async Task<Order?> GetOrderByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await GetByIdAsync<Order>(id, cancellationToken);
    }

    /// <summary>
    /// Gets all orders.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A read-only list of all orders.</returns>
    public async Task<IReadOnlyList<Order>> GetAllOrdersAsync(CancellationToken cancellationToken = default)
    {
        return await GetAllAsync<Order>(cancellationToken);
    }

    /// <summary>
    /// Adds a new order.
    /// </summary>
    /// <param name="order">The order to add.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task AddOrderAsync(Order order, CancellationToken cancellationToken = default)
    {
        await AddAsync(order, cancellationToken);
    }

    /// <summary>
    /// Updates an existing order.
    /// </summary>
    /// <param name="order">The order with updated values.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task UpdateOrderAsync(Order order, CancellationToken cancellationToken = default)
    {
        await UpdateAsync(order, cancellationToken);
    }

    /// <summary>
    /// Deletes the order with the given ID.
    /// </summary>
    /// <param name="id">The ID of the order to delete.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns><see langword="true"/> if the order was deleted; <see langword="false"/> if it did not exist.</returns>
    public async Task<bool> DeleteOrderAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DeleteAsync(_context.Orders, id, cancellationToken);
    }

    /// <summary>
    /// Gets a board by its ID with all related components eagerly loaded,
    /// or <see langword="null"/> if not found.
    /// </summary>
    /// <param name="id">The ID of the board to retrieve.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The matching board with components populated, or <see langword="null"/> if not found.</returns>
    public async Task<Board?> GetBoardWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        Board? board = await _context.Boards.FindAsync(new object[] { id }, cancellationToken);
        if (board is not null)
        {
            await _context.Entry(board)
                .Collection(b => b.BoardComponents)
                .Query()
                .Include(bc => bc.Component)
                .LoadAsync(cancellationToken);
        }

        return board;
    }

    /// <summary>
    /// Gets a board by its ID, or <see langword="null"/> if not found.
    /// </summary>
    /// <param name="id">The ID of the board to retrieve.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The matching board, or <see langword="null"/> if not found.</returns>
    public async Task<Board?> GetBoardByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await GetByIdAsync<Board>(id, cancellationToken);
    }

    /// <summary>
    /// Gets all boards.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A read-only list of all boards.</returns>
    public async Task<IReadOnlyList<Board>> GetAllBoardsAsync(CancellationToken cancellationToken = default)
    {
        return await GetAllAsync<Board>(cancellationToken);
    }

    /// <summary>
    /// Adds a new board.
    /// </summary>
    /// <param name="board">The board to add.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task AddBoardAsync(Board board, CancellationToken cancellationToken = default)
    {
        await AddAsync(board, cancellationToken);
    }

    /// <summary>
    /// Updates an existing board.
    /// </summary>
    /// <param name="board">The board with updated values.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task UpdateBoardAsync(Board board, CancellationToken cancellationToken = default)
    {
        await UpdateAsync(board, cancellationToken);
    }

    /// <summary>
    /// Deletes the board with the given ID.
    /// </summary>
    /// <param name="id">The ID of the board to delete.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns><see langword="true"/> if the board was deleted; <see langword="false"/> if it did not exist.</returns>
    public async Task<bool> DeleteBoardAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DeleteAsync(_context.Boards, id, cancellationToken);
    }

    /// <summary>
    /// Gets a component by its ID, or <see langword="null"/> if not found.
    /// </summary>
    /// <param name="id">The ID of the component to retrieve.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The matching component, or <see langword="null"/> if not found.</returns>
    public async Task<Component?> GetComponentByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await GetByIdAsync<Component>(id, cancellationToken);
    }

    /// <summary>
    /// Gets all components.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A read-only list of all components.</returns>
    public async Task<IReadOnlyList<Component>> GetAllComponentsAsync(CancellationToken cancellationToken = default)
    {
        return await GetAllAsync<Component>(cancellationToken);
    }

    /// <summary>
    /// Adds a new component.
    /// </summary>
    /// <param name="component">The component to add.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task AddComponentAsync(Component component, CancellationToken cancellationToken = default)
    {
        await AddAsync(component, cancellationToken);
    }

    /// <summary>
    /// Updates an existing component.
    /// </summary>
    /// <param name="component">The component with updated values.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task UpdateComponentAsync(Component component, CancellationToken cancellationToken = default)
    {
        await UpdateAsync(component, cancellationToken);
    }

    /// <summary>
    /// Deletes the component with the given ID.
    /// </summary>
    /// <param name="id">The ID of the component to delete.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns><see langword="true"/> if the component was deleted; <see langword="false"/> if it did not exist.</returns>
    public async Task<bool> DeleteComponentAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DeleteAsync(_context.Components, id, cancellationToken);
    }

    /// <summary>
    /// Returns <see langword="true"/> when the board with the given ID is associated with at least one order.
    /// </summary>
    /// <param name="boardId">The ID of the board to check.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns><see langword="true"/> if at least one order references this board; otherwise <see langword="false"/>.</returns>
    public async Task<bool> IsBoardAssignedToAnyOrderAsync(Guid boardId, CancellationToken cancellationToken = default)
    {
        return await _context.OrderBoards.AnyAsync(ob => ob.BoardId == boardId, cancellationToken);
    }

    /// <summary>
    /// Returns <see langword="true"/> when the component with the given ID is associated with at least one board.
    /// </summary>
    /// <param name="componentId">The ID of the component to check.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns><see langword="true"/> if at least one board references this component; otherwise <see langword="false"/>.</returns>
    public async Task<bool> IsComponentAssignedToAnyBoardAsync(Guid componentId, CancellationToken cancellationToken = default)
    {
        return await _context.BoardComponents.AnyAsync(bc => bc.ComponentId == componentId, cancellationToken);
    }

    /// <summary>
    /// Adds a board association to the order with the given ID.
    /// Has no effect if the association already exists.
    /// </summary>
    /// <param name="orderId">The ID of the order.</param>
    /// <param name="boardId">The ID of the board to associate.</param>
    /// <param name="quantity">The number of this board required for the order.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task AddBoardToOrderAsync(Guid orderId, Guid boardId, int quantity, CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.Database.ExecuteSqlInterpolatedAsync(
                $"""
                INSERT INTO "OrderBoards" ("OrderId", "BoardId", "Quantity")
                VALUES ({orderId}, {boardId}, {quantity})
                ON CONFLICT ("OrderId", "BoardId") DO UPDATE SET "Quantity" = EXCLUDED."Quantity"
                """,
                cancellationToken);
        }
        catch (Npgsql.PostgresException ex) when (ex.SqlState is "23503")
        {
            _logger.LogWarning(ex, "FK violation adding board {BoardId} to order {OrderId}; one of the entities no longer exists.", boardId, orderId);
            throw new NotFoundException($"Order {orderId} or board {boardId} no longer exists.");
        }
    }

    /// <summary>
    /// Removes the board association from the order with the given ID.
    /// </summary>
    /// <param name="orderId">The ID of the order.</param>
    /// <param name="boardId">The ID of the board to disassociate.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns><see langword="true"/> if the association was removed; <see langword="false"/> if it did not exist.</returns>
    public async Task<bool> RemoveBoardFromOrderAsync(Guid orderId, Guid boardId, CancellationToken cancellationToken = default)
    {
        int rows = await _context.Database.ExecuteSqlInterpolatedAsync(
            $"""DELETE FROM "OrderBoards" WHERE "OrderId" = {orderId} AND "BoardId" = {boardId}""",
            cancellationToken);
        return rows > 0;
    }

    /// <summary>
    /// Adds a component association to the board with the given ID, or updates the quantity if it already exists.
    /// </summary>
    /// <param name="boardId">The ID of the board.</param>
    /// <param name="componentId">The ID of the component to associate.</param>
    /// <param name="quantityPerBoard">The number of this component required per board.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task AddComponentToBoardAsync(Guid boardId, Guid componentId, int quantityPerBoard, CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.Database.ExecuteSqlInterpolatedAsync(
                $"""
                INSERT INTO "BoardComponents" ("BoardId", "ComponentId", "QuantityPerBoard")
                VALUES ({boardId}, {componentId}, {quantityPerBoard})
                ON CONFLICT ("BoardId", "ComponentId") DO UPDATE SET "QuantityPerBoard" = EXCLUDED."QuantityPerBoard"
                """,
                cancellationToken);
        }
        catch (Npgsql.PostgresException ex) when (ex.SqlState is "23503")
        {
            _logger.LogWarning(ex, "FK violation adding component {ComponentId} to board {BoardId}; one of the entities no longer exists.", componentId, boardId);
            throw new NotFoundException($"Board {boardId} or component {componentId} no longer exists.");
        }
    }

    /// <summary>
    /// Removes the component association from the board with the given ID.
    /// </summary>
    /// <param name="boardId">The ID of the board.</param>
    /// <param name="componentId">The ID of the component to disassociate.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns><see langword="true"/> if the association was removed; <see langword="false"/> if it did not exist.</returns>
    public async Task<bool> RemoveComponentFromBoardAsync(Guid boardId, Guid componentId, CancellationToken cancellationToken = default)
    {
        int rows = await _context.Database.ExecuteSqlInterpolatedAsync(
            $"""DELETE FROM "BoardComponents" WHERE "BoardId" = {boardId} AND "ComponentId" = {componentId}""",
            cancellationToken);
        return rows > 0;
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Finds an entity by its primary key, or <see langword="null"/> if not found.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="id">The primary key value.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The entity, or <see langword="null"/> if not found.</returns>
    private async Task<T?> GetByIdAsync<T>(Guid id, CancellationToken cancellationToken)
        where T : class
    {
        T? entity = await _context.Set<T>().FindAsync(new object[] { id }, cancellationToken);
        if (entity is not null)
        {
            _context.Entry(entity).State = EntityState.Detached;
        }

        return entity;
    }

    /// <summary>
    /// Returns all entities of the given type.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>All entities as a read-only list.</returns>
    private async Task<IReadOnlyList<T>> GetAllAsync<T>(CancellationToken cancellationToken)
        where T : class
    {
        return await _context.Set<T>().AsNoTracking().ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Adds an entity to the context and saves changes.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="entity">The entity to add.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    private async Task AddAsync<T>(T entity, CancellationToken cancellationToken)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(entity);
        await _context.Set<T>().AddAsync(entity, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Updates an entity in the context and saves changes.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="entity">The entity with updated values.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    private async Task UpdateAsync<T>(T entity, CancellationToken cancellationToken)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(entity);
        _context.Set<T>().Update(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Removes the entity with the given ID and saves changes.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="set">The DbSet to search and remove from.</param>
    /// <param name="id">The ID of the entity to remove.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns><see langword="true"/> if the entity was deleted; <see langword="false"/> if it did not exist.</returns>
    private async Task<bool> DeleteAsync<T>(DbSet<T> set, Guid id, CancellationToken cancellationToken)
        where T : class
    {
        T? entity = await set.FindAsync(new object[] { id }, cancellationToken);
        if (entity is null)
        {
            return false;
        }

        set.Remove(entity);
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Another thread deleted the same row concurrently — the entity is gone either way.
        }

        return true;
    }

    #endregion
}