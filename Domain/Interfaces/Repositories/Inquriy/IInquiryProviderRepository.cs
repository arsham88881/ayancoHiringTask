using Domain.Models.Inquiry.InquiryProvider;
using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Interfaces.Repositories.Inquriy;

public interface IInquiryProviderRepository
{
    Task<InquiryProviderItemOutModel[]> GetActiveProvidersAsync(int webServiceId);
}

