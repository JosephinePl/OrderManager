namespace OrderManager.Core.Exceptions;

/// <summary>
/// Thrown when a referenced entity does not exist — including when it is deleted concurrently.
/// </summary>
public sealed class NotFoundException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NotFoundException"/> class.
    /// </summary>
    /// <param name="message">A message describing which entity was not found.</param>
    public NotFoundException(string message)
        : base(message)
    {
    }
}