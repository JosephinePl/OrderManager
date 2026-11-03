namespace OrderManager.Data.Adapters;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OrderManager.Core.Entities;
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
            List<OrderBoard> orderBoards = await _context.OrderBoards
                .Where(ob => ob.OrderId == id)
                .Include(ob => ob.Board)
                .ToListAsync(cancellationToken);

            foreach (OrderBoard orderBoard in orderBoards)
            {
                List<BoardComponent> boardComponents = await _context.BoardComponents
                    .Where(bc => bc.BoardId == orderBoard.BoardId)
                    .Include(bc => bc.Component)
                    .ToListAsync(cancellationToken);

                foreach (BoardComponent boardComponent in boardComponents)
                {
                    orderBoard.Board.AddComponent(boardComponent);
                }

                order.AddBoard(orderBoard);
            }
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
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task DeleteOrderAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await DeleteAsync(_context.Orders, id, cancellationToken);
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
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task DeleteBoardAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await DeleteAsync(_context.Boards, id, cancellationToken);
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
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task DeleteComponentAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await DeleteAsync(_context.Components, id, cancellationToken);
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
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task AddBoardToOrderAsync(Guid orderId, Guid boardId, CancellationToken cancellationToken = default)
    {
        bool exists = await _context.OrderBoards
            .AnyAsync(ob => ob.OrderId == orderId && ob.BoardId == boardId, cancellationToken);
        if (!exists)
        {
            Order order = (await _context.Orders.FindAsync(new object[] { orderId }, cancellationToken))!;
            Board board = (await _context.Boards.FindAsync(new object[] { boardId }, cancellationToken))!;
            OrderBoard orderBoard = new OrderBoard(orderId, order, boardId, board);
            await _context.OrderBoards.AddAsync(orderBoard, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
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
        OrderBoard? existing = await _context.OrderBoards
            .FindAsync(new object[] { orderId, boardId }, cancellationToken);
        bool result;
        if (existing is not null)
        {
            _context.OrderBoards.Remove(existing);
            await _context.SaveChangesAsync(cancellationToken);
            result = true;
        }
        else
        {
            result = false;
        }

        return result;
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
        BoardComponent? existing = await _context.BoardComponents
            .FindAsync(new object[] { boardId, componentId }, cancellationToken);
        if (existing is not null)
        {
            _context.BoardComponents.Remove(existing);
        }

        Board board = (await _context.Boards.FindAsync(new object[] { boardId }, cancellationToken))!;
        Component component = (await _context.Components.FindAsync(new object[] { componentId }, cancellationToken))!;
        BoardComponent boardComponent = new BoardComponent(boardId, board, componentId, component, quantityPerBoard);
        await _context.BoardComponents.AddAsync(boardComponent, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
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
        BoardComponent? existing = await _context.BoardComponents
            .FindAsync(new object[] { boardId, componentId }, cancellationToken);
        bool result;
        if (existing is not null)
        {
            _context.BoardComponents.Remove(existing);
            await _context.SaveChangesAsync(cancellationToken);
            result = true;
        }
        else
        {
            result = false;
        }

        return result;
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
        return await _context.Set<T>().FindAsync(new object[] { id }, cancellationToken);
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
    /// Removes the entity with the given ID from the set and saves changes.
    /// Logs a warning if no entity with that ID exists.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="set">The DbSet to search and remove from.</param>
    /// <param name="id">The ID of the entity to remove.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    private async Task DeleteAsync<T>(DbSet<T> set, Guid id, CancellationToken cancellationToken)
        where T : class
    {
        T? entity = await set.FindAsync(new object[] { id }, cancellationToken);
        if (entity is not null)
        {
            set.Remove(entity);
            await _context.SaveChangesAsync(cancellationToken);
        }
        else
        {
            _logger.LogWarning("Attempted to delete {EntityType} with ID {Id}, but it was not found.", typeof(T).Name, id);
        }
    }

    #endregion
}