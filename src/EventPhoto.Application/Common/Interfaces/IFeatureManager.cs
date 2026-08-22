using EventPhoto.Application.Common.Models;

namespace EventPhoto.Application.Common.Interfaces;

/// <summary>
/// Centralised subscription and feature enforcement contract.
///
/// <para>All gated commands declare their requirement via <see cref="IRequiresFeature"/> and
/// are evaluated by <see cref="Behaviors.SubscriptionEnforcementBehavior{TRequest,TResponse}"/>
/// before the handler runs.</para>
///
/// <para>Implementations MUST be fail-open on transient infrastructure errors:
/// if the subscription cannot be loaded, the operation is allowed and a warning is logged.
/// Enforcement is a safety net — it must never become an availability problem.</para>
/// </summary>
public interface IFeatureManager
{
    /// <summary>Returns whether the studio can create an additional event.</summary>
    Task<FeatureCheckResult> CanCreateEventAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns whether the studio can create an additional user account.</summary>
    Task<FeatureCheckResult> CanCreateUserAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns whether a guest can start a new face search session.</summary>
    Task<FeatureCheckResult> CanStartFaceSearchAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns whether a guest can open a new upload session.</summary>
    Task<FeatureCheckResult> CanCreateGuestUploadSessionAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns whether the studio can use the branding module.</summary>
    Task<FeatureCheckResult> CanUseBrandingAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns whether the studio can use the AI Studio module.</summary>
    Task<FeatureCheckResult> CanUseAiStudioAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns whether the studio can use the Deployment Center module.</summary>
    Task<FeatureCheckResult> CanUseDeploymentCenterAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Generic entry point used by <see cref="Behaviors.SubscriptionEnforcementBehavior{TRequest,TResponse}"/>.
    /// Routes to the appropriate typed check based on <paramref name="featureKey"/>.
    /// </summary>
    Task<FeatureCheckResult> CheckFeatureAsync(string featureKey, CancellationToken cancellationToken = default);
}
