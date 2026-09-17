using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Dtos.Inquiry.InquiryProvider;

public class InquiryProviderItemDto
{
    public int Id { get; set; }

    public int WebServiceId { get; set; }
    public string EnKey { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    public bool IsEnable { get; set; }

    public short CallingPriority { get; set; }

    public string RequestMethod { get; set; } = string.Empty;

    public short TimeoutConfig { get; set; }

    public DateTime? ClosingDate { get; set; }
}