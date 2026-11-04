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
}