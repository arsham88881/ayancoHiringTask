using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Dtos.Inquiry.VehicleInquiry;

public class InquiryReqDto
{
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string? PlateNumber { get; set; }
    public bool ForceRefresh { get; set; }
}
