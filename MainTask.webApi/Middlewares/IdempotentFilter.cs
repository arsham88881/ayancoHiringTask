using Application.Services;
using Domain.Attributes.Shared;
using Domain.Models.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;

namespace MainTask.webApi.Middlewares;

public class IdempotentFilter : IAsyncActionFilter
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<IdempotentFilter> _logger;

    // قفل‌های جداگانه برای هر Idempotency-Key
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

    public IdempotentFilter(IMemoryCache cache, ILogger<IdempotentFilter> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
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
        {
            throw ResponseHelper.Failure(
                StatusCodes.Status400BadRequest,
                [new MessageItem(MessageItemContexts.Error, "Header 'Idempotency-Key' is required.", "Idempotency-Key")]);
        }

        var cacheKey = $"Idempotent_{idempotencyKey}";

        // گرفتن یا ساختن Semaphore برای این کلید
        var semaphore = _locks.GetOrAdd(cacheKey, _ => new SemaphoreSlim(1, 1));

        await semaphore.WaitAsync();
        try
        {
            // Double-check: ممکن است در زمان انتظار برای قفل، نتیجه کش شده باشد
            if (_cache.TryGetValue(cacheKey, out IdempotentResponse? cachedResponse) && cachedResponse is not null)
            {
                _logger.LogInformation("Idempotent hit for key: {Key}", idempotencyKey!);

                context.Result = new ObjectResult(cachedResponse.Value)
                {
                    StatusCode = cachedResponse.StatusCode
                };
                return;
            }

            // اجرای اکشن (فقط یک درخواست در آن واحد به اینجا می‌رسد)
            var executedContext = await next();

            // ذخیره نتیجه (فقط اگر ObjectResult بود)
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

                _logger.LogInformation(
                    "Idempotent result cached for key: {Key} (TTL: {Minutes} min)",
                    idempotencyKey,
                    attribute.CacheTimeInMinutes);
            }
        }
        finally
        {
            semaphore.Release();
        }
    }

    private class IdempotentResponse
    {
        public int StatusCode { get; set; }
        public object? Value { get; set; }
    }
}