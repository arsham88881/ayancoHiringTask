namespace Domain.Models.Inquiry.VehicleViolation;

public abstract class InquiryDto
{
    public long? Id { get; set; }
    public long? CreatedBy { get; set; }
    public DateTime? CreateDate { get; set; }
    public DateTime? CompletedDate { get; set; }
    public int? Duration { get; set; }
}
