using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Models.Inquiry.InquiryProvider;

public class SaveInquiryAttemptLogInModel
{
    public long InquiryId { get; set; }
    public int WebServiceProviderId { get; set; }
    public int RetryAttempt { get; set; }
    public bool IsLast { get; set; }
    public bool IsSuccess { get; set; }
    public int Duration { get; set; }                  // بر حسب میلی‌ثانیه
    public DateTime CreatedDate { get; set; }
    public DateTime CompletedDate { get; set; }

    public string? InputData { get; set; }              // Type = 1
    public string? OutputData { get; set; }             // Type = 2
}