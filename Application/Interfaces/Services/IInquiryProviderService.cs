using Application.Dtos.Inquiry.InquiryProvider;
using Domain.Models.Inquiry.InquiryProvider;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Interfaces.Services;

public interface IInquiryProviderService
{
    Task<(bool ReadFromCache, InquiryProviderItemDto[] ProviderList)> GetActiveProvidersAsync(int webServiceId, bool ReadAgainFromSql = false, CancellationToken ct = default);
}


