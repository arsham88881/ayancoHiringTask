namespace Domain.Models.Inquiry.VehicleViolation;

public class InquiryResultDto<OUTDATA>
{
    public InquiryStatus? Status { get; set; }
    public string? Message { get; set; }
    public OUTDATA? Data { get; set; }
    public InquiryAttemptLogDto? Log { get; set; }
}
public enum InquiryStatus
{
    /// <summary>
    /// پاسخ موفق
    /// </summary>
    Success = 1,

    /// <summary>
    /// خطای بیزینس لاجیک
    /// </summary>
    BusinessError = 2,

    /// <summary>
    /// خطای فنی مدیریت‌شده رخ‌داده در منبع خارجی
    /// </summary>
    ManagedTechnicalError = 3,

    /// <summary>
    /// خطای مدیریت‌نشده منبع خارجی
    /// </summary>
    UnhandledExternalError = 4,

    /// <summary>
    /// فرایند Timeout شد
    /// </summary>
    Timeout = 5
}