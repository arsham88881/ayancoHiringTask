using Application.Dtos.Inquiry.InquiryProvider;
using Application.Dtos.Inquiry.VehicleInquiry;
using Domain.Models.Inquiry.VehicleViolation;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Runtime;
using System.Text;

namespace Application.Interfaces.Strategies;

public interface IInquiryVehicleViolation
{
    Task<InquiryResultDto<VehicleViolationDto>> SendInquiryRequestAsync(
            InquiryProviderItemDto provider,
            InquiryReqDto request,
            int retryAttempt,
            CancellationToken ct = default);
}




public interface IInquiryFactory
{
    IInquiryVehicleViolation GetInstance(string provider);

}


