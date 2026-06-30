using EventPhoto.Contracts.Common;
using EventPhoto.Domain.Exceptions;
using FluentValidation;
using System.Net;
using System.Text.Json;

namespace EventPhoto.Api.Middleware;

/// <summary>
/// Global exception handling middleware that converts unhandled exceptions into structured JSON responses.
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExceptionHandlingMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next pipeline delegate.</param>
    /// <param name="logger">The logger instance.</param>
    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// Executes the middleware.
    /// </summary>
    /// <param name="context">The current HTTP context.</param>
    /// <returns>A task that completes when request processing finishes.</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation exception encountered.");
            await WriteResponseAsync(
                context,
                HttpStatusCode.BadRequest,
                new ApiResponse
                {
                    Success = false,
                    Error = "Validation failed.",
                    ValidationErrors = ex.Errors
                        .GroupBy(error => error.PropertyName)
                        .ToDictionary(group => group.Key, group => group.Select(error => error.ErrorMessage).ToArray())
                });
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning(ex, "Requested resource was not found.");
            await WriteResponseAsync(context, HttpStatusCode.NotFound, ApiResponse.Fail(ex.Message));
        }
        catch (BusinessRuleException ex)
        {
            _logger.LogWarning(ex, "Business rule violation encountered: {Rule}", ex.Rule);
            await WriteResponseAsync(context, HttpStatusCode.UnprocessableEntity, ApiResponse.Fail(ex.Message));
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain validation failure encountered.");
            await WriteResponseAsync(context, HttpStatusCode.BadRequest, ApiResponse.Fail(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception encountered.");
            await WriteResponseAsync(context, HttpStatusCode.InternalServerError, ApiResponse.Fail("An unexpected error occurred."));
        }
    }

    private static async Task WriteResponseAsync(HttpContext context, HttpStatusCode statusCode, object response)
    {
        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.Clear();
        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(response, SerializerOptions));
    }
}
