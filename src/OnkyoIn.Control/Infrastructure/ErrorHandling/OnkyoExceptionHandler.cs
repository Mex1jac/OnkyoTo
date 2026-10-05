using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using OnkyoIn.Control.Domain.Exceptions;

namespace OnkyoIn.Control.Infrastructure.ErrorHandling;

/// <summary>
/// Central exception handler that maps domain exceptions to standard
/// <see cref="ProblemDetails"/> HTTP responses.
/// </summary>
public class OnkyoExceptionHandler : IExceptionHandler
{
    private readonly ILogger<OnkyoExceptionHandler> _logger;

    public OnkyoExceptionHandler(ILogger<OnkyoExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, title) = exception switch
        {
            DeviceNotFoundException => (StatusCodes.Status404NotFound, "Device not found"),
            DeviceCommunicationException => (StatusCodes.Status502BadGateway, "Device communication error"),
            ArgumentException => (StatusCodes.Status400BadRequest, "Invalid request"),
            _ => (StatusCodes.Status500InternalServerError, "Unexpected error"),
        };

        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception while processing {Path}", httpContext.Request.Path);
        }
        else
        {
            _logger.LogWarning(exception, "{Title}: {Message}", title, exception.Message);
        }

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = exception.Message,
            Instance = httpContext.Request.Path,
        };

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);

        return true;
    }
}
