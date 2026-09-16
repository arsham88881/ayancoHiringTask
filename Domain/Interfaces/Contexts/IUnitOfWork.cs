using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace Domain.Interfaces.Contexts;

public interface IUnitOfWork : IAsyncDisposable, IDisposable
{
    //// دسترسی به کانکشن و تراکنش فعال (برای استفاده در Dapper)
    IDbConnection Connection { get; }
    IDbTransaction? Transaction { get; }
    //// وضعیت
    IReadOnlyList<string> ActiveSavepoints { get; }
    bool HasActiveTransaction { get; }
    string TransactionId { get; }

    //// مدیریت تراکنش
    Task BeginTransactionAsync(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted, CancellationToken cancellationToken = default);
    Task CommitAsync(CancellationToken cancellationToken = default);
    Task RollbackAsync(CancellationToken cancellationToken = default);

    //Task<string> CreateSavepointAsync(string name, CancellationToken cancellationToken = default);
    //Task RollbackToSavepointAsync(string name, CancellationToken cancellationToken = default);
    void EnsureConnectionOpen();
    Task EnsureConnectionOpenAsync(CancellationToken cancellationToken = default);
    Task ReleaseConnectionAsync(CancellationToken cancellationToken = default);

    /// کمکی های دریافت repository
    Task<TRepository> GetRepositoryAsync<TRepository>(CancellationToken ct = default) where TRepository : class;
    TRepository GetRepository<TRepository>() where TRepository : class;
}
