namespace OrderManager.Core.Entities;

/// <summary>
/// Represents an component placed on a board.
/// </summary>
/// <param name="Id">The ID of the component.</param>
/// <param name="Name">The name of the component.</param>
/// <param name="Description">A description of the component.</param>
/// <param name="Quantity">The available quantity of the component.</param>
public sealed record Component(Guid Id, string Name, string Description, int Quantity)
{
    /// <summary>
    /// Backing store for <see cref="BoardComponents"/>.
    /// </summary>
    private readonly List<BoardComponent> _boardComponents = [];

    #region Properties

    /// <summary>
    /// Gets the board associations for this component.
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
    /// Adds a board association to this component.
    /// </summary>
    /// <param name="boardComponent">The board association to add.</param>
    public void AddBoard(BoardComponent boardComponent)
    {
        _boardComponents.Add(boardComponent);
    }

    /// <summary>
    /// Removes the board association with the given board ID from this component.
    /// </summary>
    /// <param name="boardId">The ID of the board whose association to remove.</param>
    public void RemoveBoard(Guid boardId)
    {
        _boardComponents.RemoveAll(bc => bc.BoardId == boardId);
    }

    /// <summary>
    /// Determines whether the specified <see cref="Component"/> is equal to the current instance.
    /// </summary>
    /// <param name="other">The <see cref="Component"/> to compare with the current instance.</param>
    /// <returns><see langword="true"/> if the specified <see cref="Component"/> has the same <see cref="Id"/>; otherwise, <see langword="false"/>.</returns>
    public bool Equals(Component? other)
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