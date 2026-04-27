using System.Net;
using System.Text.Json;

namespace TechStore.API.Middleware;

public class ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger, IHostEnvironment env)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await HandleAsync(context, ex);
        }
    }

    private Task HandleAsync(HttpContext context, Exception ex)
    {
        var (status, message, isExpected) = ex switch
        {
            UnauthorizedAccessException  => (HttpStatusCode.Unauthorized,          ex.Message,                       true),
            KeyNotFoundException         => (HttpStatusCode.NotFound,               ex.Message,                       true),
            InvalidOperationException    => (HttpStatusCode.BadRequest,             ex.Message,                       true),
            ArgumentException            => (HttpStatusCode.BadRequest,             ex.Message,                       true),
            OperationCanceledException   => (HttpStatusCode.BadRequest,             "The request was cancelled.",     true),
            _                            => (HttpStatusCode.InternalServerError,    "An unexpected error occurred.",  false),
        };

        if (isExpected)
            // Business rule violations are expected — no stack trace needed
            logger.LogWarning("{Method} {Path} → {Status}: {Message}",
                context.Request.Method, context.Request.Path, (int)status, ex.Message);
        else
            // Truly unexpected — log the full exception with stack trace
            logger.LogError(ex, "{Method} {Path} → 500: {Message}",
                context.Request.Method, context.Request.Path, ex.Message);

        // In development, expose the real error for 500s; in production return a safe message
        var responseMessage = (!isExpected && env.IsDevelopment()) ? ex.Message : message;

        context.Response.ContentType = "application/json";
        context.Response.StatusCode  = (int)status;

        return context.Response.WriteAsync(
            JsonSerializer.Serialize(new { message = responseMessage }));
    }
}
