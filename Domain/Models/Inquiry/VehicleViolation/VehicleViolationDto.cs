using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Models.Inquiry.VehicleViolation;

public class VehicleViolationDto : InquiryDto
{
    public long? ApplicantId { get; set; }
    public string? PlateNumber { get; set; }
    public long? TotalAmount { get; set; }
    public string? Paymentld { get; set; }
    public string? BillId { get; set; }
    public int? Count { get; set; }

}
