namespace OrderManager.Core.Entities;

/// <summary>
/// Represents the many-to-many relationship between an <see cref="Order"/> and a <see cref="Board"/>.
/// </summary>
/// <param name="OrderId">The ID of the associated order.</param>
/// <param name="Order">The associated order.</param>
/// <param name="BoardId">The ID of the associated board.</param>
/// <param name="Board">The associated board.</param>
public sealed record OrderBoard(Guid OrderId, Order Order, Guid BoardId, Board Board)
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OrderBoard"/> class for use by EF Core materialization.
    /// </summary>
    private OrderBoard()
        : this(default, null!, default, null!)
    {
    }
}