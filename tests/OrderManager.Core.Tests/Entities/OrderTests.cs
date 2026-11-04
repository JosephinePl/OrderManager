namespace OrderManager.Core.Tests.Entities;

using OrderManager.Core.Entities;

/// <summary>
/// Unit tests for <see cref="Order"/>.
/// </summary>
[TestClass]
public sealed class OrderTests
{
    #region Methods

    /// <summary>
    /// Adding a board association appends it to <see cref="Order.OrderBoards"/>.
    /// </summary>
    [TestMethod]
    public void WhenAddBoardIsCalledAndBoardIsNew_ThenOrderBoardsContainsBoard()
    {
        Order order = CreateOrder();
        OrderBoard orderBoard = CreateOrderBoard(order);

        order.AddBoard(orderBoard);

        bool result = order.OrderBoards.Contains(orderBoard);
        Assert.IsTrue(result);
    }

    /// <summary>
    /// Removing an existing board association by its ID removes it from <see cref="Order.OrderBoards"/>.
    /// </summary>
    [TestMethod]
    public void WhenRemoveBoardIsCalledAndBoardExists_ThenOrderBoardsNoLongerContainsBoard()
    {
        Order order = CreateOrder();
        OrderBoard orderBoard = CreateOrderBoard(order);
        order.AddBoard(orderBoard);

        order.RemoveBoard(orderBoard.BoardId);

        bool result = order.OrderBoards.Contains(orderBoard);
        Assert.IsFalse(result);
    }

    /// <summary>
    /// Removing a board that was never added leaves <see cref="Order.OrderBoards"/> unchanged.
    /// </summary>
    [TestMethod]
    public void WhenRemoveBoardIsCalledAndBoardDoesNotExist_ThenOrderBoardsIsUnchanged()
    {
        Order order = CreateOrder();
        OrderBoard orderBoard = CreateOrderBoard(order);
        order.AddBoard(orderBoard);

        order.RemoveBoard(Guid.NewGuid());

        int result = order.OrderBoards.Count;
        Assert.AreEqual(1, result);
    }

    /// <summary>
    /// Two orders with the same ID are equal.
    /// </summary>
    [TestMethod]
    public void WhenEqualsAndOtherHasSameId_ThenResultIsTrue()
    {
        Guid id = Guid.NewGuid();
        Order order = CreateOrder(id);
        Order other = CreateOrder(id);

        bool result = order.Equals(other);

        Assert.IsTrue(result);
    }

    /// <summary>
    /// Two orders with different IDs are not equal.
    /// </summary>
    [TestMethod]
    public void WhenEqualsAndOtherHasDifferentId_ThenResultIsFalse()
    {
        Order order = CreateOrder();
        Order other = CreateOrder();

        bool result = order.Equals(other);

        Assert.IsFalse(result);
    }

    /// <summary>
    /// An order compared to <see langword="null"/> is not equal.
    /// </summary>
    [TestMethod]
    public void WhenEqualsAndOtherIsNull_ThenResultIsFalse()
    {
        Order order = CreateOrder();

        bool result = order.Equals(null);

        Assert.IsFalse(result);
    }

    /// <summary>
    /// Two orders with the same ID produce the same hash code.
    /// </summary>
    [TestMethod]
    public void WhenGetHashCodeAndSameId_ThenHashCodesAreEqual()
    {
        Guid id = Guid.NewGuid();
        Order order = CreateOrder(id);
        Order other = CreateOrder(id);

        int orderHash = order.GetHashCode();
        int otherHash = other.GetHashCode();

        Assert.AreEqual(orderHash, otherHash);
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Creates an <see cref="Order"/> with optional ID override.
    /// </summary>
    /// <param name="id">The ID to assign; a new <see cref="Guid"/> is used when <see langword="null"/>.</param>
    /// <returns>A new <see cref="Order"/> instance.</returns>
    private static Order CreateOrder(Guid? id = null)
    {
        Order result = new(id ?? Guid.NewGuid(), "Order", "Description", DateTimeOffset.UtcNow);
        return result;
    }

    /// <summary>
    /// Creates an <see cref="OrderBoard"/> linking a new <see cref="Board"/> to the given <paramref name="order"/>.
    /// </summary>
    /// <param name="order">The order to link.</param>
    /// <returns>A new <see cref="OrderBoard"/> instance.</returns>
    private static OrderBoard CreateOrderBoard(Order order)
    {
        Board board = new(Guid.NewGuid(), "Board", "Description", 100m, 50m);
        OrderBoard result = new(order.Id, board.Id, board, 1);
        return result;
    }

    #endregion
}