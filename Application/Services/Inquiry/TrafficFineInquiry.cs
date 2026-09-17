using Application.Dtos.Inquiry.VehicleInquiry;
using Application.Interfaces.Services;
using Application.Interfaces.Strategies;
using Domain.Enums;
using Domain.Interfaces.Repositories.Inquriy;
using Domain.Models.Inquiry.VehicleViolation;
using Domain.Models.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Application.Services.Inquiry;


public class TrafficFineInquiry(
    IInquiryFactory inquiryFactory,
    IInquiryProviderService providerService,
    IInquiryVehicleViolationRepository inquiryRepository,
    IMemoryCache cache,
    ILogger<TrafficFineInquiry> logger)
{
    private const int ResultCacheMinutes = 5;

    public async Task<(MessageItem[]? warns, InquiryResultDto<VehicleViolationDto> res)> ExecuteAsync(
        InquiryReqDto model,
        CancellationToken ct = default)
    {
        var cacheKey = GenerateResultCacheKey(model);
        var warnings = new List<MessageItem>();
        var attemptLogs = new List<InquiryAttemptLogDto>();

        // ========== ۱. بررسی کش نتیجه استعلام ==========
        if (!model.ForceRefresh && cache.TryGetValue(cacheKey, out InquiryResultDto<VehicleViolationDto>? cachedResult)
            && cachedResult is not null)
        {
            logger.LogInformation("Inquiry result loaded from cache. Plate: {Plate}", model.PlateNumber);
            return (null, cachedResult);
        }

        if (model.ForceRefresh)
        {
            cache.Remove(cacheKey);
            logger.LogInformation("Force refresh requested. Cache cleared for Plate: {Plate}", model.PlateNumber);
        }

        // ========== ۲. دریافت لیست Providerهای فعال ==========
        var (_, providers) = await providerService.GetActiveProvidersAsync(
              (int)WebServices.InquiryVehicleViolationSummery,
              model.ForceRefresh,
              ct);

        if (providers is null || providers.Length == 0)
            throw ResponseHelper.Failure(StatusCodes.Status400BadRequest, [new MessageItem(MessageItemContexts.Error, "هیچ ارائه‌دهنده فعالی برای این سرویس یافت نشد.", "BusinessError")]);

        // مرتب‌سازی بر اساس اولویت (CallingPriority)
        var orderedProviders = providers.OrderBy(x => x.CallingPriority).ToArray();

        InquiryResultDto<VehicleViolationDto>? finalResult = null;

        // ========== ۳. حلقه روی Providerها ==========
        for (int i = 0; i < orderedProviders.Length; i++)
        {
            var provider = orderedProviders[i];
            var retryAttempt = (i + 1);

            try
            {
                var service = inquiryFactory.GetInstance(provider.EnKey);

                var result = await service.SendInquiryRequestAsync(
                    provider,
                    model,
                    retryAttempt,
                    ct);

                if (result.Log is not null)
                    attemptLogs.Add(result.Log);

                // اگر پاسخ قابل قبول بود (موفق یا خطای بیزینسی)
                if (result.Status is InquiryStatus.Success or InquiryStatus.BusinessError)
                {
                    finalResult = result;
                    break;
                }

                // پاسخ ناموفق 
                warnings.Add(new MessageItem(MessageItemContexts.Warning, $"Provider [{provider.EnKey}] failed. Status: {result.Status} - {result.Message}", result.Status?.ToString() ?? "Unknown"));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unhandled error while calling Provider: {Provider}", provider.EnKey);
                warnings.Add(new MessageItem(MessageItemContexts.Warning, $"Provider [{provider.EnKey}] threw exception: {ex.Message}", "UnhandledException"));
            }
        }

        // ========== ۴. اگر هیچ Provider موفق نبود ==========
        if (finalResult is null)
        {
            warnings.Add(new MessageItem(MessageItemContexts.Warning, "هیچ ارائه‌دهنده‌ای پاسخ معتبر نداد", "ManagedTechnicalError"));
            throw ResponseHelper.Failure(StatusCodes.Status204NoContent, warnings.ToArray());
        }


        if (warnings.Count > 0)
            logger.LogWarning("Inquiry finished with {Count} warnings for Plate: {Plate}", warnings.Count, model.PlateNumber);



        // ========== ۵. ذخیره یک‌باره در دیتابیس ==========
        await SaveAllAsync(model, finalResult, attemptLogs, ct);


        if (finalResult.Status is InquiryStatus.Success or InquiryStatus.BusinessError)
        {
            var cacheOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(ResultCacheMinutes));

            cache.Set(cacheKey, finalResult, cacheOptions);
        }


        return (warnings.Count > 0 ? warnings.ToArray() : null, finalResult);
    }

    private long UserId = 11;

    private async Task SaveAllAsync(
       InquiryReqDto model,
       InquiryResultDto<VehicleViolationDto> result,
       List<InquiryAttemptLogDto> attemptLogs,
       CancellationToken ct)
    {
        try
        {
            var now = DateTime.UtcNow;

            var saveModel = new VehicleViolationInModel
            {
                // Header
                InquiryStatusId = (byte)(result.Status ?? InquiryStatus.ManagedTechnicalError),
                CreatedBy = UserId,
                CreateDate = now,
                CompletedDate = now,
                Duration = attemptLogs.Sum(x => x.Duration),
                InquiryTypeId = (short)WebServices.InquiryVehicleViolationSummery,

                // Detail
                ApplicantId = UserId,
                PlateNumber = model.PlateNumber ?? string.Empty,
                TotalAmount = result.Data?.TotalAmount,
                BillId = result.Data?.BillId,
                PaymentId = result.Data?.Paymentld,
                Count = (short?)result.Data?.Count,

                // Attempt Logs
                AttemptLogs = attemptLogs
            };

            await inquiryRepository.SaveInquiryAsync(saveModel);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to save inquiry + attempts for Plate: {Plate}", model.PlateNumber);
        }
    }


    private static int userId = 1;
    private static string GenerateResultCacheKey(InquiryReqDto model)
        => $"InquiryResult:VehicleViolation:{model.PlateNumber}:{userId}";
}