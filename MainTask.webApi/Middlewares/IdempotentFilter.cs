using Application.Services;
using Domain.Attributes.Shared;
using Domain.Models.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Memory;

namespace MainTask.webApi.Middlewares;

public class IdempotentFilter : IAsyncActionFilter
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<IdempotentFilter> _logger;

    public IdempotentFilter(IMemoryCache cache, ILogger<IdempotentFilter> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // فقط برای متدهایی که Attribute دارند
        var attribute = context.ActionDescriptor.EndpointMetadata
            .OfType<IdempotentAttribute>()
            .FirstOrDefault();

        if (attribute is null)
        {
            await next();
            return;
        }

        // خواندن کلید از Header
        if (!context.HttpContext.Request.Headers.TryGetValue("Idempotency-Key", out var idempotencyKey)
            || string.IsNullOrWhiteSpace(idempotencyKey))
            throw ResponseHelper.Failure(StatusCodes.Status400BadRequest, [new MessageItem(MessageItemContexts.Error, "Header 'Idempotency-Key' is required.", "Idempotency-Key")]);

        var cacheKey = $"Idempotent_{idempotencyKey}";

        // اگر قبلاً این کلید دیده شده → نتیجه قبلی را برگردان
        if (_cache.TryGetValue(cacheKey, out IdempotentResponse? cachedResponse) && cachedResponse is not null)
        {
            _logger.LogInformation("Idempotent hit for key: {Key}", idempotencyKey);

            context.Result = new ObjectResult(cachedResponse.Value)
            {
                StatusCode = cachedResponse.StatusCode
            };
            return;
        }

        // اجرای اکشن
        var executedContext = await next();

        // ذخیره نتیجه (فقط اگر موفق بود)
        if (executedContext.Result is ObjectResult objectResult)
        {
            var responseToCache = new IdempotentResponse
            {
                StatusCode = objectResult.StatusCode ?? 200,
                Value = objectResult.Value
            };

            var cacheOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(attribute.CacheTimeInMinutes))
                .SetPriority(CacheItemPriority.Normal);

            _cache.Set(cacheKey, responseToCache, cacheOptions);

            _logger.LogInformation("Idempotent result cached for key: {Key} (TTL: {Minutes} min)",
                idempotencyKey, attribute.CacheTimeInMinutes);
        }
    }

    private class IdempotentResponse
    {
        public int StatusCode { get; set; }
        public object? Value { get; set; }
    }
}