using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using PayTR.PosSelection.Infrastructure.Models.Exceptions;

namespace PayTR.PosSelection.Infrastructure.Middlewares;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext httpContext)
    {
        try
        {
            await _next(httpContext);
        }
        catch (IpRateLimitException ex)
        {
            _logger.LogError(ex, ex.Message);
            await HandleExceptionAsync(httpContext, ex.GetType().Name, ex.Message, ex.StatusCode, ex)
                .ConfigureAwait(false);
        }
        catch (ValidationException ex)
        {
            _logger.LogError(ex, ex.Errors[0].Item2);
            await HandleExceptionAsync(httpContext, ex.GetType().Name, ex.Errors[0].Item2, ex.Errors[0].Item1, ex)
                .ConfigureAwait(false);
        }
        catch (CustomErrorException ex)
        {
            _logger.LogError(ex, ex.Message);
            await HandleExceptionAsync(httpContext, ex.Type, ex.Message, ex.StatusCode, ex).ConfigureAwait(false);
        }
        catch (BadRequestException ex)
        {
            _logger.LogError(ex, ex.Message);
            await HandleExceptionAsync(httpContext, ex.GetType().Name, ex.Message, ex.StatusCode, ex)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(httpContext, ex.GetType().Name, ex.Message, 400, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, string type, string ex, int statusCode,
        Exception exception)
    {
        var response = new
        {
            success = false,
            error = new
            {
                message = ex,
                type = type,
                status = statusCode,
                traceId = context.TraceIdentifier
            }
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;

        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}