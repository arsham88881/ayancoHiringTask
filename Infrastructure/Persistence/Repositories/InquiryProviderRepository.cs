using Application.Exceptions;
using Dapper;
using Domain.Interfaces.Contexts;
using Domain.Interfaces.Repositories.Inquriy;
using Domain.Models.Inquiry.InquiryProvider;
using System.Data.Common;

namespace Infrastructure.Persistence.Repositories;

public class InquiryProviderRepository(IUnitOfWork uow) : IInquiryProviderRepository
{
    public async Task<InquiryProviderItemOutModel[]> GetActiveProvidersAsync(int webServiceId)
    {
        try
        {
            await uow.EnsureConnectionOpenAsync().ConfigureAwait(false);

            const string sql = """
                SELECT
                    [WP].[Id]
                  , [WP].[WebServiceId]
                  , [WP].[EnKey]
                  , [WP].[Address]
                  , [WP].[IsEnable]
                  , [WP].[CallingPriority]
                  , [WP].[RequestMethod]
                  , [WP].[TimeoutConfig]
                  , [WP].[ClosingDate]
                FROM [Meta].[WebServiceProvider] [WP]
                WHERE
                    [WP].[WebServiceId] = @WebServiceId
                    AND [WP].[IsEnable] = 1
                    AND ([ClosingDate] IS NULL OR [ClosingDate] > GETDATE());
                -- ORDER BY [CallingPriority] ASC;
                """;

            var result = await uow.Connection.QueryAsync<InquiryProviderItemOutModel>(
                sql,
                new { WebServiceId = webServiceId }
            ).ConfigureAwait(false);

            return result?.ToArray() ?? [];
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
