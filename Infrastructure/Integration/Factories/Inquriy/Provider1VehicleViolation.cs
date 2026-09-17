using Application.Dtos.Inquiry.InquiryProvider;
using Application.Dtos.Inquiry.VehicleInquiry;
using Application.Interfaces.Strategies;
using Domain.Interfaces.Contexts;
using Domain.Models.Inquiry.VehicleViolation;
using Domain.Models.Shared;
using RestSharp;

namespace Infrastructure.Integration.Factories.Inquriy;

public class Provider1VehicleViolation : IInquiryVehicleViolation
{
    private readonly IHttpIntegrationContext _httpIntegrationContext;

    public Provider1VehicleViolation(
        IHttpIntegrationContext httpIntegrationContext)
    {
        _httpIntegrationContext = httpIntegrationContext;
    }

    public async Task<InquiryResultDto<VehicleViolationDto>> SendInquiryRequestAsync(
        InquiryProviderItemDto provider,
        InquiryReqDto request,
        int retryAttempt,
        CancellationToken ct = default)
    {

        var startDate = DateTime.UtcNow;
        string? inputData = null;
        string? outputData = null;

        InquiryStatus finalStatus = InquiryStatus.UnhandledExternalError;
        string? message = null;
        VehicleViolationDto? mappedData = null;

        inputData = System.Text.Json.JsonSerializer.Serialize(request);

        var restRequest = new RestRequestDto
        {
            EndPoint = "1/vehicle-violations/inquiry",
            BodyParams = request
        };

        var options = new RestAdvanceOptions
        {
            TimeoutConfig = provider.TimeoutConfig,
            DisableSsl = true,
            RetryCount = retryAttempt
        };

        var response = await _httpIntegrationContext.PostAsync<Provider1Response>(
            baseUrlAddress: provider.Address,
            request: restRequest,
            options: options,
            cancellationToken: ct);

        outputData = response.Content;

        if (response.ResponseStatus == ResponseStatus.TimedOut)
        {
            finalStatus = InquiryStatus.Timeout;
            message = "درخواست به دلیل Timeout ناموفق بود";
        }
        else if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
        {
            finalStatus = InquiryStatus.BusinessError;
            message = response.Value?.Message ?? "خطای بیزینسی از سمت ارائه‌دهنده";
        }
        else if ((int)response.StatusCode >= 500)
        {
            finalStatus = InquiryStatus.ManagedTechnicalError;
            message = response.Value?.Message ?? "خطای فنی از سمت ارائه‌دهنده";
        }
        else if (response.IsSuccessful && response.Value?.Success == true)
        {
            finalStatus = InquiryStatus.Success;
            message = "استعلام با موفقیت انجام شد";

            mappedData = new VehicleViolationDto
            {
                PlateNumber = response.Value.Data?.PlateNumber,
                TotalAmount = response.Value.Data?.TotalAmount,
                BillId = response.Value.Data?.BillId,
                Paymentld = response.Value.Data?.PaymentId,
                Count = response.Value.Data?.Count
            };
        }
        else
        {
            finalStatus = InquiryStatus.ManagedTechnicalError;
            message = response.ErrorMessage ?? "پاسخ نامعتبر از ارائه‌دهنده";
        }



        var endDate = DateTime.UtcNow;
        var duration = (int)(endDate - startDate).TotalMilliseconds;

        // فقط لاگ را می‌سازیم و برمی‌گردانیم (بدون ذخیره در دیتابیس)
        var log = new InquiryAttemptLogDto
        {
            WebServiceProviderId = provider.Id,
            RetryAttempt = response.RestResponseLogData!.RetryAttempt,
            IsSuccess = finalStatus == InquiryStatus.Success,
            Duration = duration,
            CreatedDate = startDate,
            CompletedDate = endDate,
            InputData = inputData,
            OutputData = outputData,
            Status = finalStatus,
            Message = message
        };

        return new InquiryResultDto<VehicleViolationDto>
        {
            Status = finalStatus,
            Message = message,
            Data = mappedData,
            Log = log
        };

    }

    // ========== مدل‌های پاسخ Provider1 ==========
    private class Provider1Response
    {
        public bool Success { get; set; }
        public string? Provider { get; set; }
        public string? Message { get; set; }
        public Provider1Data? Data { get; set; }
    }

    private class Provider1Data
    {
        public string? PlateNumber { get; set; }
        public long? TotalAmount { get; set; }
        public string? BillId { get; set; }
        public string? PaymentId { get; set; }
        public int? Count { get; set; }
    }
}