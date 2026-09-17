using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Models.Inquiry.InquiryProvider;

public class InquiryProviderItemOutModel
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