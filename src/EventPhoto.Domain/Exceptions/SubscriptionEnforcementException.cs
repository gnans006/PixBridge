namespace EventPhoto.Domain.Exceptions;

/// <summary>
/// Thrown when an operation is blocked by subscription enforcement — i.e., the studio's
/// current plan does not permit the requested action.
///
/// <para>Maps to HTTP 402 Payment Required in <see cref="EventPhoto.Api.Middleware.ExceptionHandlingMiddleware"/>.</para>
/// </summary>
public sealed class SubscriptionEnforcementException : Exception
{
    /// <summary>The feature key that was denied (one of <c>FeatureKey.*</c> constants).</summary>
    public string FeatureKey { get; }

    /// <summary>Initializes a new instance with a user-facing denial message.</summary>
    public SubscriptionEnforcementException(string featureKey, string message)
        : base(message)
    {
        FeatureKey = featureKey;
    }
}
