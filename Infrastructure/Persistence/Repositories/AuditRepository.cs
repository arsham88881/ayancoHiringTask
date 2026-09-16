using Application.Exceptions;
using Dapper;
using Domain.Interfaces.Contexts;
using Domain.Interfaces.Repositories;
using Domain.Models.Audit;
using System.Data;
using System.Data.Common;

namespace Infrastructure.Persistence.Repositories;

public class AuditRepository(IUnitOfWork uow) : IAuditRepository
{
    public async Task SaveEventLog(SaveEventInModel evetParam)
    {
        try
        {
            await uow.EnsureConnectionOpenAsync().ConfigureAwait(false);
            await uow.Connection.ExecuteAsync("[Audit].[S_Event_Insert]", evetParam, uow.Transaction, 500, CommandType.StoredProcedure);
        }
        catch (DbException ex)
        {
            throw new InfrastructureException(ex, "خطایی پایگاه داده لطفا با پشتیبانی ارتبط بگیرید");
        }
        catch (Exception ex)
        {
            throw;
        }
    }
}
