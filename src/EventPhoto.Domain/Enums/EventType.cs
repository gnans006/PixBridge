namespace EventPhoto.Domain.Enums;

/// <summary>
/// Type of photography event.
/// </summary>
public enum EventType
{
    /// <summary>
    /// Wedding ceremony.
    /// </summary>
    Wedding = 1,

    /// <summary>
    /// Wedding reception.
    /// </summary>
    Reception = 2,

    /// <summary>
    /// Birthday celebration.
    /// </summary>
    Birthday = 3,

    /// <summary>
    /// Corporate event.
    /// </summary>
    Corporate = 4,

    /// <summary>
    /// Outdoor photoshoot or event.
    /// </summary>
    Outdoor = 5,

    /// <summary>
    /// Any other event type.
    /// </summary>
    Other = 6
}
