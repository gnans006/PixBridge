using EventPhoto.Application.Common.Interfaces;
using EventPhoto.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace EventPhoto.Application.ApplicationSettings.Commands;

/// <summary>Tests whether a given URL is reachable from the server.</summary>
/// <param name="Url">The URL to probe.</param>
public sealed record ValidatePublicBaseUrlCommand(string Url) : IRequest<Result<UrlValidationResult>>;

/// <summary>Result of a public URL connectivity check.</summary>
public sealed record UrlValidationResult(
    bool IsReachable,
    int? StatusCode,
    long? ResponseTimeMs,
    string? ErrorMessage);

/// <summary>Handles <see cref="ValidatePublicBaseUrlCommand"/>.</summary>
public sealed class ValidatePublicBaseUrlCommandHandler(
    IUrlReachabilityService reachabilityService,
    ILogger<ValidatePublicBaseUrlCommandHandler> logger)
    : IRequestHandler<ValidatePublicBaseUrlCommand, Result<UrlValidationResult>>
{
    /// <inheritdoc />
    public async Task<Result<UrlValidationResult>> Handle(
        ValidatePublicBaseUrlCommand request,
        CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(request.Url?.Trim(), UriKind.Absolute, out var uri) ||
            uri.Scheme is not ("http" or "https"))
        {
            return Result.Success(new UrlValidationResult(
                IsReachable: false,
                StatusCode: null,
                ResponseTimeMs: null,
                ErrorMessage: "URL must be a valid http:// or https:// address."));
        }

        logger.LogDebug("Testing URL reachability: {Url}", request.Url);
        var probe = await reachabilityService.TestAsync(request.Url!, cancellationToken);

        return Result.Success(new UrlValidationResult(
            probe.IsReachable,
            probe.StatusCode,
            probe.ResponseTimeMs,
            probe.ErrorMessage));
    }
}
