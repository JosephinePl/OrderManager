namespace OrderManager.Core.Tests.Entities;

using OrderManager.Core.Entities;

/// <summary>
/// Unit tests for <see cref="Component"/>.
/// </summary>
[TestClass]
public sealed class ComponentTests
{
    #region Methods

    /// <summary>
    /// Adding a board association appends it to <see cref="Component.BoardComponents"/>.
    /// </summary>
    [TestMethod]
    public void WhenAddBoardIsCalledAndBoardIsNew_ThenBoardComponentsContainsBoard()
    {
        Component component = CreateComponent();
        BoardComponent boardComponent = CreateBoardComponent(component);

        component.AddBoard(boardComponent);

        bool result = component.BoardComponents.Contains(boardComponent);
        Assert.IsTrue(result);
    }

    /// <summary>
    /// Removing an existing board association by its ID removes it from <see cref="Component.BoardComponents"/>.
    /// </summary>
    [TestMethod]
    public void WhenRemoveBoardIsCalledAndBoardExists_ThenBoardComponentsNoLongerContainsBoard()
    {
        Component component = CreateComponent();
        BoardComponent boardComponent = CreateBoardComponent(component);
        component.AddBoard(boardComponent);

        component.RemoveBoard(boardComponent.BoardId);

        bool result = component.BoardComponents.Contains(boardComponent);
        Assert.IsFalse(result);
    }

    /// <summary>
    /// Removing a board that was never added leaves <see cref="Component.BoardComponents"/> unchanged.
    /// </summary>
    [TestMethod]
    public void WhenRemoveBoardIsCalledAndBoardDoesNotExist_ThenBoardComponentsIsUnchanged()
    {
        Component component = CreateComponent();
        BoardComponent boardComponent = CreateBoardComponent(component);
        component.AddBoard(boardComponent);

        component.RemoveBoard(Guid.NewGuid());

        int result = component.BoardComponents.Count;
        Assert.AreEqual(1, result);
    }

    /// <summary>
    /// Two components with the same ID are equal.
    /// </summary>
    [TestMethod]
    public void WhenEqualsAndOtherHasSameId_ThenResultIsTrue()
    {
        Guid id = Guid.NewGuid();
        Component component = CreateComponent(id);
        Component other = CreateComponent(id);

        bool result = component.Equals(other);

        Assert.IsTrue(result);
    }

    /// <summary>
    /// Two components with different IDs are not equal.
    /// </summary>
    [TestMethod]
    public void WhenEqualsAndOtherHasDifferentId_ThenResultIsFalse()
    {
        Component component = CreateComponent();
        Component other = CreateComponent();

        bool result = component.Equals(other);

        Assert.IsFalse(result);
    }

    /// <summary>
    /// A component compared to <see langword="null"/> is not equal.
    /// </summary>
    [TestMethod]
    public void WhenEqualsAndOtherIsNull_ThenResultIsFalse()
    {
        Component component = CreateComponent();

        bool result = component.Equals(null);

        Assert.IsFalse(result);
    }

    /// <summary>
    /// Two components with the same ID produce the same hash code.
    /// </summary>
    [TestMethod]
    public void WhenGetHashCodeAndSameId_ThenHashCodesAreEqual()
    {
        Guid id = Guid.NewGuid();
        Component component = CreateComponent(id);
        Component other = CreateComponent(id);

        int componentHash = component.GetHashCode();
        int otherHash = other.GetHashCode();

        Assert.AreEqual(componentHash, otherHash);
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Creates a <see cref="Component"/> with optional ID override.
    /// </summary>
    /// <param name="id">The ID to assign; a new <see cref="Guid"/> is used when <see langword="null"/>.</param>
    /// <returns>A new <see cref="Component"/> instance.</returns>
    private static Component CreateComponent(Guid? id = null)
    {
        Component result = new(id ?? Guid.NewGuid(), "Component", "Description", 10);
        return result;
    }

    /// <summary>
    /// Creates a <see cref="BoardComponent"/> linking a new <see cref="Board"/> to the given <paramref name="component"/>.
    /// </summary>
    /// <param name="component">The component to link.</param>
    /// <returns>A new <see cref="BoardComponent"/> instance.</returns>
    private static BoardComponent CreateBoardComponent(Component component)
    {
        Board board = new(Guid.NewGuid(), "Board", "Description", 100m, 50m);
        BoardComponent result = new(board.Id, board, component.Id, component, 2);
        return result;
    }

    #endregion
}