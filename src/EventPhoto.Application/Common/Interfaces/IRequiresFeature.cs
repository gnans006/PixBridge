namespace EventPhoto.Application.Common.Interfaces;

/// <summary>
/// Marker interface implemented by MediatR request types (commands) that require
/// a subscription feature check before execution.
///
/// <para>Commands should implement this interface and return the appropriate
/// <see cref="Models.FeatureKey"/> constant from <see cref="FeatureKey"/>.</para>
///
/// <example>
/// <code>
/// public sealed record CreateEventCommand(...) : IRequest&lt;Result&lt;Guid&gt;&gt;, IRequiresFeature
/// {
///     public string FeatureKey => Models.FeatureKey.Events;
/// }
/// </code>
/// </example>
/// </summary>
public interface IRequiresFeature
{
    /// <summary>
    /// The feature key to evaluate. Must be one of the constants defined in
    /// <see cref="Models.FeatureKey"/>.
    /// </summary>
    string FeatureKey { get; }
}
