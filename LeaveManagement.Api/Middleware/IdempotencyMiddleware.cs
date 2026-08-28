using Microsoft.Extensions.Caching.Memory;
using System.Net;

namespace LeaveManagement.Api.Middleware;

public class IdempotencyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IMemoryCache _cache;

    public IdempotencyMiddleware(RequestDelegate next, IMemoryCache cache)
    {
        _next = next;
        _cache = cache;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Only enforce idempotency on mutating state endpoints (POST, PUT, DELETE)
        if (HttpMethods.IsGet(context.Request.Method))
        {
            await _next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue("X-Idempotency-Key", out var idempotencyKey) || string.IsNullOrWhiteSpace(idempotencyKey))
        {
            await _next(context);
            return;
        }

        string cacheKey = $"Idempotency_{idempotencyKey}";

        if (_cache.TryGetValue(cacheKey, out string? previousResponse))
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.OK;
            await context.Response.WriteAsync(previousResponse!);
            return;
        }

        // Capture response to cache standard responses
        var originalResponseBodyStream = context.Response.Body;
        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        await _next(context);

        if (context.Response.StatusCode is >= 200 and < 300)
        {
            responseBody.Seek(0, SeekOrigin.Begin);
            string responseContent = await new StreamReader(responseBody).ReadToEndAsync();
            responseBody.Seek(0, SeekOrigin.Begin);

            _cache.Set(cacheKey, responseContent, TimeSpan.FromMinutes(10));
        }

        await responseBody.CopyToAsync(originalResponseBodyStream);
    }
}