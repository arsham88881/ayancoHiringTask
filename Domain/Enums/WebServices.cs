using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Enums;

public enum WebServices
{
    /// <summary>
    /// استعلام خلافی خودرو به صورت خلاصه 
    /// </summary>
    InquiryVehicleViolationSummery = 1,
    /// <summary>
    /// استعلام خلافی خودرو با جزئیات
    /// </summary>
    InquiryVehicleViolationWithDetail = 2
}

