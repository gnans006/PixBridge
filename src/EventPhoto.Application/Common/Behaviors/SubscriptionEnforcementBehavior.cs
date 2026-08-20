using EventPhoto.Application.Common.Interfaces;
using EventPhoto.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace EventPhoto.Application.Common.Behaviors;

/// <summary>
/// MediatR pipeline behavior that evaluates subscription feature enforcement for any
/// command that implements <see cref="IRequiresFeature"/>.
///
/// <para>Pipeline position: runs AFTER <see cref="ValidationBehavior{TRequest,TResponse}"/>
/// to ensure only structurally valid requests reach the enforcement check.</para>
///
/// <para>Throws <see cref="SubscriptionEnforcementException"/> (HTTP 402) when the check
/// fails. The <see cref="ExceptionHandlingMiddleware"/> in the API layer handles the mapping.</para>
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public sealed class SubscriptionEnforcementBehavior<TRequest, TResponse>(
    IFeatureManager featureManager,
    ILogger<SubscriptionEnforcementBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    /// <inheritdoc />
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Only intercept commands that declare a feature requirement
        if (request is not IRequiresFeature gatedRequest)
        {
            return await next();
        }

        var featureKey = gatedRequest.FeatureKey;

        logger.LogDebug(
            "Evaluating subscription enforcement for feature '{Feature}' (command={Command}).",
            featureKey,
            typeof(TRequest).Name);

        var result = await featureManager.CheckFeatureAsync(featureKey, cancellationToken);

        if (!result.IsAllowed)
        {
            logger.LogWarning(
                "Subscription enforcement denied feature '{Feature}' for command '{Command}': {Reason}",
                featureKey,
                typeof(TRequest).Name,
                result.Reason);

            throw new SubscriptionEnforcementException(featureKey, result.Reason!);
        }

        return await next();
    }
}
