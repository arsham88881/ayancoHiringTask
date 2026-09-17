using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Models.Inquiry.VehicleViolation;

public class VehicleViolationInModel
{
    // Header
    public byte InquiryStatusId { get; set; }
    public long CreatedBy { get; set; }
    public DateTime CreateDate { get; set; }
    public DateTime CompletedDate { get; set; }
    public int Duration { get; set; }
    public short InquiryTypeId { get; set; }

    // Detail
    public long? ApplicantId { get; set; }
    public string PlateNumber { get; set; } = string.Empty;
    public long? TotalAmount { get; set; }
    public string? BillId { get; set; }
    public string? PaymentId { get; set; }
    public short? Count { get; set; }

    // Attempt Logs
    public List<InquiryAttemptLogDto> AttemptLogs { get; set; } = new();
}