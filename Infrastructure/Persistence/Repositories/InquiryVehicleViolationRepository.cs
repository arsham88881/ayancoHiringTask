using Application.Exceptions;
using Dapper;
using Domain.Interfaces.Contexts;
using Domain.Interfaces.Repositories.Inquriy;
using Domain.Models.Inquiry.VehicleViolation;
using System.Data;
using System.Data.Common;

namespace Infrastructure.Persistence.Repositories;

public class InquiryVehicleViolationRepository(IUnitOfWork uow) : IInquiryVehicleViolationRepository
{
    public async Task<long> SaveInquiryAsync(VehicleViolationInModel model)
    {
        try
        {
            await uow.EnsureConnectionOpenAsync().ConfigureAwait(false);

            var attemptLogsJson = model.AttemptLogs is { Count: > 0 }
                ? System.Text.Json.JsonSerializer.Serialize(model.AttemptLogs)
                : null;

            var parameters = new
            {
                model.InquiryStatusId,
                model.CreatedBy,
                model.CreateDate,
                model.CompletedDate,
                model.Duration,
                model.InquiryTypeId,

                model.ApplicantId,
                model.PlateNumber,
                model.TotalAmount,
                model.BillId,
                model.PaymentId,
                model.Count,

                AttemptLogsJson = attemptLogsJson
            };

            var inquiryId = await uow.Connection.ExecuteScalarAsync<long>(
                "[Inquiry].[S_VehicleViolation_Insert]",
                parameters,
                uow.Transaction,
                commandTimeout: 30,
                commandType: CommandType.StoredProcedure
            ).ConfigureAwait(false);

            return inquiryId;
        }
        catch (DbException ex)
        {
            throw new InfrastructureException(ex, "خطایی پایگاه داده لطفا با پشتیبانی ارتباط بگیرید");
        }
        catch (Exception)
        {
            throw;
        }
    }
}