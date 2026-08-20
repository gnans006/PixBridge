using EventPhoto.Application.Common.Interfaces;
using EventPhoto.Application.Common.Models;
using EventPhoto.Domain.Enums;
using EventPhoto.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace EventPhoto.Infrastructure.Services.Subscription;

/// <summary>
/// Centralised subscription feature enforcement implementation.
///
/// <para>Design principles:</para>
/// <list type="bullet">
///   <item>Fail-open on infrastructure errors — if the subscription cannot be loaded, allow
///   the operation and log a warning. Enforcement must never become an availability problem.</item>
///   <item>Limit checks are COUNT-based via single <c>COUNT(*)</c> queries — no full entity loads.</item>
///   <item>All checks are async and <see cref="CancellationToken"/>-aware.</item>
///   <item>Zero scattered <c>if(plan == Trial)</c> logic — everything routes through this class.</item>
/// </list>
/// </summary>
public sealed class FeatureManager(
    ISubscriptionRepository subscriptionRepository,
    IEventRepository eventRepository,
    IUserRepository userRepository,
    ILogger<FeatureManager> logger)
    : IFeatureManager
{
    /// <inheritdoc />
    public async Task<FeatureCheckResult> CanCreateEventAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var subscription = await subscriptionRepository.GetAsync(cancellationToken);
            if (subscription is null)
            {
                logger.LogWarning("FeatureManager: subscription record not found; allowing CreateEvent (fail-open).");
                return FeatureCheckResult.Allow();
            }

            if (!subscription.IsOperational)
            {
                return FeatureCheckResult.Deny(
                    FeatureDenialCode.SubscriptionExpired,
                    BuildExpiredMessage(subscription.State));
            }

            // 0 = unlimited
            if (subscription.MaxEvents == 0)
                return FeatureCheckResult.Allow();

            var currentCount = await eventRepository.CountAsync(cancellationToken);

            if (currentCount >= subscription.MaxEvents)
            {
                return FeatureCheckResult.Deny(
                    FeatureDenialCode.LimitReached,
                    $"Your {subscription.Plan.ToDisplayName()} plan allows a maximum of {subscription.MaxEvents} event(s). " +
                    $"You have reached this limit. Upgrade your plan to create more events.");
            }

            return FeatureCheckResult.Allow();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "FeatureManager: unexpected error evaluating CanCreateEvent; allowing (fail-open).");
            return FeatureCheckResult.Allow();
        }
    }

    /// <inheritdoc />
    public async Task<FeatureCheckResult> CanCreateUserAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var subscription = await subscriptionRepository.GetAsync(cancellationToken);
            if (subscription is null)
            {
                logger.LogWarning("FeatureManager: subscription record not found; allowing CreateUser (fail-open).");
                return FeatureCheckResult.Allow();
            }

            if (!subscription.IsOperational)
            {
                return FeatureCheckResult.Deny(
                    FeatureDenialCode.SubscriptionExpired,
                    BuildExpiredMessage(subscription.State));
            }

            // 0 = unlimited
            if (subscription.MaxUsersPerStudio == 0)
                return FeatureCheckResult.Allow();

            var currentCount = await userRepository.CountActiveAsync(cancellationToken);

            if (currentCount >= subscription.MaxUsersPerStudio)
            {
                return FeatureCheckResult.Deny(
                    FeatureDenialCode.LimitReached,
                    $"Your {subscription.Plan.ToDisplayName()} plan allows a maximum of {subscription.MaxUsersPerStudio} user(s). " +
                    $"You have reached this limit. Upgrade your plan to add more users.");
            }

            return FeatureCheckResult.Allow();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "FeatureManager: unexpected error evaluating CanCreateUser; allowing (fail-open).");
            return FeatureCheckResult.Allow();
        }
    }

    /// <inheritdoc />
    public async Task<FeatureCheckResult> CanStartFaceSearchAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var subscription = await subscriptionRepository.GetAsync(cancellationToken);
            if (subscription is null)
            {
                logger.LogWarning("FeatureManager: subscription record not found; allowing FaceSearch (fail-open).");
                return FeatureCheckResult.Allow();
            }

            // Face search is unlimited on Trial — only blocked when subscription is no longer operational
            if (!subscription.IsOperational)
            {
                return FeatureCheckResult.Deny(
                    FeatureDenialCode.SubscriptionExpired,
                    "Face search is not available — the studio's subscription has expired. " +
                    "Please contact the studio for assistance.");
            }

            return FeatureCheckResult.Allow();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "FeatureManager: unexpected error evaluating CanStartFaceSearch; allowing (fail-open).");
            return FeatureCheckResult.Allow();
        }
    }

    /// <inheritdoc />
    public async Task<FeatureCheckResult> CanCreateGuestUploadSessionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var subscription = await subscriptionRepository.GetAsync(cancellationToken);
            if (subscription is null)
            {
                logger.LogWarning("FeatureManager: subscription record not found; allowing GuestUpload (fail-open).");
                return FeatureCheckResult.Allow();
            }

            if (!subscription.IsOperational)
            {
                return FeatureCheckResult.Deny(
                    FeatureDenialCode.SubscriptionExpired,
                    "Guest uploads are not available — the studio's subscription has expired.");
            }

            return FeatureCheckResult.Allow();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "FeatureManager: unexpected error evaluating CanCreateGuestUploadSession; allowing (fail-open).");
            return FeatureCheckResult.Allow();
        }
    }

    /// <inheritdoc />
    public Task<FeatureCheckResult> CheckFeatureAsync(string featureKey, CancellationToken cancellationToken = default)
        => featureKey switch
        {
            FeatureKey.Events              => CanCreateEventAsync(cancellationToken),
            FeatureKey.Users               => CanCreateUserAsync(cancellationToken),
            FeatureKey.FaceSearchSessions  => CanStartFaceSearchAsync(cancellationToken),
            FeatureKey.GuestUploadSessions => CanCreateGuestUploadSessionAsync(cancellationToken),
            _ => Task.FromResult(FeatureCheckResult.Allow()), // unknown keys are allowed — forward compat
        };

    private static string BuildExpiredMessage(SubscriptionState state) => state switch
    {
        SubscriptionState.Expired   => "Your subscription has expired. Existing data is safe, but new resources cannot be created. Please activate a new license to continue.",
        SubscriptionState.Cancelled => "Your subscription has been cancelled. Existing data is safe, but new resources cannot be created. Please contact support.",
        _                           => "Your subscription is not active. Please contact support.",
    };
}

/// <summary>Extension helpers for <see cref="SubscriptionPlan"/> display names.</summary>
internal static class SubscriptionPlanExtensions
{
    internal static string ToDisplayName(this SubscriptionPlan plan) => plan switch
    {
        SubscriptionPlan.Trial         => "Trial",
        SubscriptionPlan.ExtendedTrial => "Extended Trial",
        SubscriptionPlan.Professional  => "Professional",
        SubscriptionPlan.Premium       => "Premium",
        _                              => plan.ToString(),
    };
}
