using Domain.Models.Inquiry.VehicleViolation;

namespace Domain.Interfaces.Repositories.Inquriy;

public interface IInquiryVehicleViolationRepository
{
    Task<long> SaveInquiryAsync(VehicleViolationInModel model);

}