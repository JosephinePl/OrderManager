namespace OrderManager.Core.Entities;

/// <summary>
/// Represents a board used in manufacturing.
/// </summary>
/// <param name="Id">The ID of the board.</param>
/// <param name="Name">The name of the board.</param>
/// <param name="Description">A description of the board.</param>
/// <param name="Length">The length of the board, in mm.</param>
/// <param name="Width">The width of the board, in mm.</param>
public sealed record Board(Guid Id, string Name, string Description, decimal Length, decimal Width)
{
    /// <summary>
    /// Backing store for <see cref="OrderBoards"/>.
    /// </summary>
    private readonly List<OrderBoard> _orderBoards = [];

    /// <summary>
    /// Backing store for <see cref="BoardComponents"/>.
    /// </summary>
    private readonly List<BoardComponent> _boardComponents = [];

    #region Properties

    /// <summary>
    /// Gets the order associations for this board.
    /// </summary>
    public IReadOnlyList<OrderBoard> OrderBoards
    {
        get
        {
            return _orderBoards;
        }
    }

    /// <summary>
    /// Gets the component associations for this board.
    /// </summary>
    public IReadOnlyList<BoardComponent> BoardComponents
    {
        get
        {
            return _boardComponents;
        }
    }

    #endregion

    #region Methods

    /// <summary>
    /// Adds an order association to this board.
    /// </summary>
    /// <param name="orderBoard">The order association to add.</param>
    public void AddOrder(OrderBoard orderBoard)
    {
        _orderBoards.Add(orderBoard);
    }

    /// <summary>
    /// Removes the order association with the given order ID from this board.
    /// </summary>
    /// <param name="orderId">The ID of the order whose association to remove.</param>
    public void RemoveOrder(Guid orderId)
    {
        _orderBoards.RemoveAll(ob => ob.OrderId == orderId);
    }

    /// <summary>
    /// Adds a component association to this board.
    /// </summary>
    /// <param name="boardComponent">The component association to add.</param>
    public void AddComponent(BoardComponent boardComponent)
    {
        _boardComponents.Add(boardComponent);
    }

    /// <summary>
    /// Removes the component association with the given component ID from this board.
    /// </summary>
    /// <param name="componentId">The ID of the component whose association to remove.</param>
    public void RemoveComponent(Guid componentId)
    {
        _boardComponents.RemoveAll(bc => bc.ComponentId == componentId);
    }

    /// <summary>
    /// Determines whether the specified <see cref="Board"/> is equal to the current instance.
    /// </summary>
    /// <param name="other">The <see cref="Board"/> to compare with the current instance.</param>
    /// <returns><see langword="true"/> if the specified <see cref="Board"/> has the same <see cref="Id"/>; otherwise, <see langword="false"/>.</returns>
    public bool Equals(Board? other)
    {
        bool result = other is not null && Id == other.Id;
        return result;
    }

    /// <summary>
    /// Returns a hash code for the current instance.
    /// </summary>
    /// <returns>A hash code based on the <see cref="Id"/> of the current instance.</returns>
    public override int GetHashCode()
    {
        int result = Id.GetHashCode();
        return result;
    }

    #endregion
}