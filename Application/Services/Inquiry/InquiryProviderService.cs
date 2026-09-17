using Application.Dtos.Inquiry.InquiryProvider;
using Application.Interfaces.Services;
using Domain.Interfaces.Repositories.Inquriy;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Application.Services.Inquiry;

public class InquiryProviderService(
    IInquiryProviderRepository providerRepository,
    ILogger<InquiryProviderService> logger,
    IMemoryCache cache) : IInquiryProviderService
{
    private const int CacheMinutes = 5;

    public async Task<(bool ReadFromCache, InquiryProviderItemDto[] ProviderList)>
        GetActiveProvidersAsync(
            int webServiceId,
            bool readAgainFromSql = false,
            CancellationToken ct = default)
    {
        var cacheKey = GenerateCacheKey(webServiceId);

        // اگر کاربر خواست دوباره از SQL بخونه → کش رو پاک کن
        if (readAgainFromSql)
        {
            cache.Remove(cacheKey);
            logger.LogInformation("Cache cleared for WebServiceId: {WebServiceId}", webServiceId);
        }

        // اول سعی کن از کش بخونی
        if (cache.TryGetValue(cacheKey, out InquiryProviderItemDto[]? cachedProviders) && cachedProviders is not null)
        {
            logger.LogInformation("Providers loaded from Cache for WebServiceId: {WebServiceId}", webServiceId);
            return (true, cachedProviders);
        }

        // از دیتابیس بخون
        var providers = await providerRepository.GetActiveProvidersAsync(webServiceId).ConfigureAwait(false);

        var result = providers
            .Select(p => new InquiryProviderItemDto
            {
                Id = p.Id,
                WebServiceId = p.WebServiceId,
                Address = p.Address,
                EnKey = p.EnKey,
                IsEnable = p.IsEnable,
                CallingPriority = p.CallingPriority,
                RequestMethod = p.RequestMethod,
                TimeoutConfig = p.TimeoutConfig,
                ClosingDate = p.ClosingDate
            })
            .ToArray();

        // کش کن برای ۵ دقیقه
        var cacheOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(TimeSpan.FromMinutes(CacheMinutes))
            .SetPriority(CacheItemPriority.Normal);

        cache.Set(cacheKey, result, cacheOptions);

        logger.LogInformation("Providers loaded from SQL and cached for WebServiceId: {WebServiceId}", webServiceId);

        return (false, result);
    }

    private static string GenerateCacheKey(int webServiceId)
        => $"InquiryProviders:WebServiceId:{webServiceId}";
}