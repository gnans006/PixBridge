using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace EventPhoto.Application.Common.Behaviors;

/// <summary>
/// MediatR pipeline behavior that logs the execution time of every request.
/// Requests that take longer than 500 ms are logged as warnings.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public sealed class LoggingBehavior<TRequest, TResponse>(
    ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private const int SlowRequestThresholdMs = 500;

    /// <inheritdoc />
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;

        logger.LogInformation("Handling {RequestType}", requestName);

        var sw = Stopwatch.StartNew();
        TResponse response;

        try
        {
            response = await next();
        }
        catch (Exception ex)
        {
            sw.Stop();
            logger.LogError(
                ex,
                "Request {RequestType} failed after {ElapsedMs} ms",
                requestName,
                sw.ElapsedMilliseconds);
            throw;
        }

        sw.Stop();

        if (sw.ElapsedMilliseconds > SlowRequestThresholdMs)
        {
            logger.LogWarning(
                "Slow request detected: {RequestType} took {ElapsedMs} ms",
                requestName,
                sw.ElapsedMilliseconds);
        }
        else
        {
            logger.LogInformation(
                "Handled {RequestType} in {ElapsedMs} ms",
                requestName,
                sw.ElapsedMilliseconds);
        }

        return response;
    }
}
