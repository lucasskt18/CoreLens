namespace CoreLens.Api.Auth;

public sealed class AgentTokenMiddleware
{
    public const string HeaderName = "X-Agent-Token";

    private readonly RequestDelegate _next;
    private readonly string _token;

    public AgentTokenMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;
        _token = configuration["Agent:Token"] ?? "dev-local-token-change-me";
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments("/internal"))
        {
            if (!context.Request.Headers.TryGetValue(HeaderName, out var provided) ||
                !string.Equals(provided.ToString(), _token, StringComparison.Ordinal))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("Invalid agent token.");
                return;
            }
        }

        await _next(context);
    }
}
