namespace TechStore.API.Middleware;

public class CorrelationIdMiddleware(RequestDelegate next)
{
    private const string Header = "X-Correlation-ID";

    public async Task InvokeAsync(HttpContext context)
    {
        var id = context.Request.Headers[Header].FirstOrDefault()
                 ?? Guid.NewGuid().ToString("N")[..16];

        context.Items["CorrelationId"] = id;
        context.Response.Headers[Header] = id;

        await next(context);
    }
}
