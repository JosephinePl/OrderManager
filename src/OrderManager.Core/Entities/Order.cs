namespace OrderManager.Core.Entities;

/// <summary>
/// Represents a manufacturing order.
/// </summary>
/// <param name="Id">The ID of the order.</param>
/// <param name="Name">The name of the order.</param>
/// <param name="Description">A description of the order.</param>
/// <param name="OrderDate">The timestamp at which the order was placed.</param>
public sealed record Order(Guid Id, string Name, string Description, DateTimeOffset OrderDate)
{
    /// <summary>
    /// Backing store for <see cref="OrderBoards"/>.
    /// </summary>
    private readonly List<OrderBoard> _orderBoards = [];

    #region Properties

    /// <summary>
    /// Gets the board associations for this order.
    /// </summary>
    public IReadOnlyList<OrderBoard> OrderBoards
    {
        get
        {
            return _orderBoards;
        }
    }

    #endregion

    #region Methods

    /// <summary>
    /// Adds a board association to this order.
    /// </summary>
    /// <param name="orderBoard">The board association to add.</param>
    public void AddBoard(OrderBoard orderBoard)
    {
        _orderBoards.Add(orderBoard);
    }

    /// <summary>
    /// Removes the board association with the given board ID from this order.
    /// </summary>
    /// <param name="boardId">The ID of the board whose association to remove.</param>
    public void RemoveBoard(Guid boardId)
    {
        _orderBoards.RemoveAll(ob => ob.BoardId == boardId);
    }

    /// <summary>
    /// Determines whether the specified <see cref="Order"/> is equal to the current instance.
    /// </summary>
    /// <param name="other">The <see cref="Order"/> to compare with the current instance.</param>
    /// <returns><see langword="true"/> if the specified <see cref="Order"/> has the same <see cref="Id"/>; otherwise, <see langword="false"/>.</returns>
    public bool Equals(Order? other)
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