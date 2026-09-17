namespace Domain.Models.Inquiry.VehicleViolation;

public class InquiryAttemptLogDto
{
    public int WebServiceProviderId { get; set; }
    public int RetryAttempt { get; set; }
    public bool IsSuccess { get; set; }
    public int Duration { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime CompletedDate { get; set; }
    public string? InputData { get; set; }
    public string? OutputData { get; set; }
    public InquiryStatus Status { get; set; }
    public string? Message { get; set; }
}
