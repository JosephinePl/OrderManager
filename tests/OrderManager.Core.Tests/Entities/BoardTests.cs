namespace OrderManager.Core.Tests.Entities;

using OrderManager.Core.Entities;

/// <summary>
/// Unit tests for <see cref="Board"/>.
/// </summary>
[TestClass]
public sealed class BoardTests
{
    #region Methods

    /// <summary>
    /// Adding a component association appends it to <see cref="Board.BoardComponents"/>.
    /// </summary>
    [TestMethod]
    public void WhenAddComponentIsCalledAndComponentIsNew_ThenBoardComponentsContainsComponent()
    {
        Board board = CreateBoard();
        BoardComponent boardComponent = CreateBoardComponent(board);

        board.AddComponent(boardComponent);

        bool result = board.BoardComponents.Contains(boardComponent);
        Assert.IsTrue(result);
    }

    /// <summary>
    /// Removing an existing component association by its ID removes it from <see cref="Board.BoardComponents"/>.
    /// </summary>
    [TestMethod]
    public void WhenRemoveComponentIsCalledAndComponentExists_ThenBoardComponentsNoLongerContainsComponent()
    {
        Board board = CreateBoard();
        BoardComponent boardComponent = CreateBoardComponent(board);
        board.AddComponent(boardComponent);

        board.RemoveComponent(boardComponent.ComponentId);

        bool result = board.BoardComponents.Contains(boardComponent);
        Assert.IsFalse(result);
    }

    /// <summary>
    /// Removing a component that was never added leaves <see cref="Board.BoardComponents"/> unchanged.
    /// </summary>
    [TestMethod]
    public void WhenRemoveComponentIsCalledAndComponentDoesNotExist_ThenBoardComponentsIsUnchanged()
    {
        Board board = CreateBoard();
        BoardComponent boardComponent = CreateBoardComponent(board);
        board.AddComponent(boardComponent);

        board.RemoveComponent(Guid.NewGuid());

        int result = board.BoardComponents.Count;
        Assert.AreEqual(1, result);
    }

    /// <summary>
    /// Two boards with the same ID are equal.
    /// </summary>
    [TestMethod]
    public void WhenEqualsAndOtherHasSameId_ThenResultIsTrue()
    {
        Guid id = Guid.NewGuid();
        Board board = CreateBoard(id);
        Board other = CreateBoard(id);

        bool result = board.Equals(other);

        Assert.IsTrue(result);
    }

    /// <summary>
    /// Two boards with different IDs are not equal.
    /// </summary>
    [TestMethod]
    public void WhenEqualsAndOtherHasDifferentId_ThenResultIsFalse()
    {
        Board board = CreateBoard();
        Board other = CreateBoard();

        bool result = board.Equals(other);

        Assert.IsFalse(result);
    }

    /// <summary>
    /// A board compared to <see langword="null"/> is not equal.
    /// </summary>
    [TestMethod]
    public void WhenEqualsAndOtherIsNull_ThenResultIsFalse()
    {
        Board board = CreateBoard();

        bool result = board.Equals(null);

        Assert.IsFalse(result);
    }

    /// <summary>
    /// Two boards with the same ID produce the same hash code.
    /// </summary>
    [TestMethod]
    public void WhenGetHashCodeAndSameId_ThenHashCodesAreEqual()
    {
        Guid id = Guid.NewGuid();
        Board board = CreateBoard(id);
        Board other = CreateBoard(id);

        int boardHash = board.GetHashCode();
        int otherHash = other.GetHashCode();

        Assert.AreEqual(boardHash, otherHash);
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Creates a <see cref="Board"/> with optional ID override.
    /// </summary>
    /// <param name="id">The ID to assign; a new <see cref="Guid"/> is used when <see langword="null"/>.</param>
    /// <returns>A new <see cref="Board"/> instance.</returns>
    private static Board CreateBoard(Guid? id = null)
    {
        Board result = new(id ?? Guid.NewGuid(), "Board", "Description", 100m, 50m);
        return result;
    }

    /// <summary>
    /// Creates an <see cref="OrderBoard"/> linking the given <paramref name="board"/> to a new <see cref="Order"/>.
    /// </summary>
    /// <param name="board">The board to link.</param>
    /// <returns>A new <see cref="OrderBoard"/> instance.</returns>
    private static OrderBoard CreateOrderBoard(Board board)
    {
        Order order = new(Guid.NewGuid(), "Order", "Description", DateTimeOffset.UtcNow);
        OrderBoard result = new(order.Id, board.Id, board, 1);
        return result;
    }

    /// <summary>
    /// Creates a <see cref="BoardComponent"/> linking the given <paramref name="board"/> to a new <see cref="Component"/>.
    /// </summary>
    /// <param name="board">The board to link.</param>
    /// <returns>A new <see cref="BoardComponent"/> instance.</returns>
    private static BoardComponent CreateBoardComponent(Board board)
    {
        Component component = new(Guid.NewGuid(), "Component", "Description", 10);
        BoardComponent result = new(board.Id, component.Id, component, 2);
        return result;
    }

    #endregion
}