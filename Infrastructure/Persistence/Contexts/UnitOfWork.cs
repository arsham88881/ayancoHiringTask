using Application.Exceptions;
using Domain.Interfaces.Contexts;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Text;

namespace Infrastructure.Persistence.Contexts;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly IDbFactoryContext _connectionFactory;
    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<Type, object> _repositories;
    private readonly ILogger<UnitOfWork> _logger;
    private readonly SemaphoreSlim _connectionLock = new(1, 1);

    private SqlConnection? _connection;
    private SqlTransaction? _transaction;
    private bool _disposed;

    private readonly List<string> _savepoints = new();
    private readonly List<object> _domainEvents = new();
    private string? _transactionId;

    // قابل تنظیم توسط caller؛ پیش‌فرض ADO.NET (30s)
    public int? CommandTimeoutSeconds { get; set; } = 60;

    public UnitOfWork(IDbFactoryContext connectionFactory, ILogger<UnitOfWork> logger, IServiceProvider serviceProvider)
    {
        _repositories = new Dictionary<Type, object>();
        _connectionFactory = connectionFactory;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    // Properties
    public IDbConnection Connection
    {
        get
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(UnitOfWork));

            if (_connection == null)
                throw new InfrastructureException($"{nameof(UnitOfWork)}: Connection has not been initialized. Call EnsureConnectionOpen() or EnsureConnectionOpenAsync() first.");

            if (_connection.State != ConnectionState.Open)
                throw new InvalidOperationException($"Connection is not open. Current state: {_connection.State}. Call EnsureConnectionOpen() before accessing.");

            return _connection;
        }
    }


    public IDbTransaction? Transaction => _transaction;
    public bool HasActiveTransaction => _transaction != null;
    public string TransactionId => _transactionId ?? "None";
    public IReadOnlyList<string> ActiveSavepoints => _savepoints.AsReadOnly();


    // --------------------------------------------------------------
    //  همزمان (Sync)
    // --------------------------------------------------------------
    public void EnsureConnectionOpen()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(UnitOfWork));

        _connectionLock.Wait();
        try
        {
            if (_connection == null)
            {
                _connection = _connectionFactory.CreateConnection();
                _logger.LogDebug("Connection instance created.");
            }

            if (_connection.State == ConnectionState.Broken)
            {
                _logger.LogWarning("Connection is broken, recreating...");
                try { _connection.Dispose(); } catch { }
                _connection = _connectionFactory.CreateConnection();
            }

            if (_connection.State == ConnectionState.Closed)
            {
                _connection.Open();
                _logger.LogDebug("Connection opened (sync).");
            }
            else if (_connection.State == ConnectionState.Connecting)
            {
                var timeout = DateTime.UtcNow.AddSeconds(30);
                while (_connection.State == ConnectionState.Connecting && DateTime.UtcNow < timeout)
                    Thread.Sleep(50);

                if (_connection.State != ConnectionState.Open)
                    throw new InvalidOperationException("Connection stuck in 'Connecting' state.");
            }
        }
        finally
        {
            _connectionLock.Release();
        }
    }


    // --------------------------------------------------------------
    //  ناهمزمان (Async) با پشتیبانی از CancellationToken
    // --------------------------------------------------------------
    // نسخهٔ Public — خودش لاک می‌گیرد (برای فراخوانی مستقیم از بیرون کلاس)
    public async Task EnsureConnectionOpenAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (_disposed) throw new ObjectDisposedException(nameof(UnitOfWork));

        await _connectionLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await EnsureConnectionOpenAsyncCore(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    // نسخهٔ Core — بدون لاک؛ فقط از داخل متدهایی صدا زده شود که قبلاً لاک را گرفته‌اند
    private async Task EnsureConnectionOpenAsyncCore(CancellationToken cancellationToken)
    {
        if (_connection == null)
        {
            _connection = _connectionFactory.CreateConnection();
            _logger.LogDebug("Connection instance created.");
        }

        if (_connection.State == ConnectionState.Broken)
        {
            _logger.LogWarning("Connection is broken, recreating...");
            try { await _connection.DisposeAsync(); } catch { }
            _connection = _connectionFactory.CreateConnection();
        }

        if (_connection.State == ConnectionState.Closed)
        {
            const int maxRetries = 3;

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    await _connection.OpenAsync(cancellationToken).ConfigureAwait(false);
                    _logger.LogDebug("Connection opened (async), attempt {Attempt}.", attempt);
                    break;
                }
                catch (SqlException ex) when (IsPoolTimeout(ex) && attempt < maxRetries)
                {
                    _logger.LogWarning(
                        "Connection pool exhausted (attempt {Attempt}/{Max}). Retrying in {Delay}ms...",
                        attempt, maxRetries, 200 * attempt);

                    await Task.Delay(TimeSpan.FromMilliseconds(200 * attempt), cancellationToken)
                        .ConfigureAwait(false);
                }
                catch (SqlException ex) when (IsPoolTimeout(ex))
                {
                    _logger.LogError(ex,
                        "Connection pool exhausted after {Max} attempts. Consider increasing MaxPoolSize or investigating long-held connections.",
                        maxRetries);
                    throw;
                }
            }
        }
        else if (_connection.State == ConnectionState.Connecting)
        {
            var timeout = TimeSpan.FromSeconds(30);
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(timeout);

            while (_connection.State == ConnectionState.Connecting && !cts.IsCancellationRequested)
                await Task.Delay(50, cts.Token).ConfigureAwait(false);

            if (_connection.State != ConnectionState.Open)
                throw new OperationCanceledException("Connection stuck in 'Connecting' state.");
        }
    }
    // --------------------------------------------------------------
    //  تشخیص خطای Pool Timeout (جدا از سایر SqlException ها)
    // --------------------------------------------------------------
    private static bool IsPoolTimeout(SqlException ex) =>
        ex.Number == -2 ||
        ex.Message.Contains("Timeout expired", StringComparison.OrdinalIgnoreCase);

    // --------------------------------------------------------------
    //  مدیریت تراکنش
    // --------------------------------------------------------------
    public async Task BeginTransactionAsync(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted, CancellationToken cancellationToken = default)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(UnitOfWork));

        await _connectionLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_transaction != null)
                throw new InvalidOperationException(
                    $"{nameof(UnitOfWork)}: A transaction is already active (Id: {TransactionId}). ");

            await EnsureConnectionOpenAsyncCore(cancellationToken).ConfigureAwait(false);

            _transaction = (SqlTransaction)await _connection!.BeginTransactionAsync(isolationLevel, cancellationToken).ConfigureAwait(false);
            _transactionId = Guid.NewGuid().ToString("N");
            _savepoints.Clear();

            _logger.LogInformation("Transaction started. Id: {TransactionId}, Isolation: {Isolation}", _transactionId, isolationLevel);
        }
        finally
        {
            _connectionLock.Release();
        }
    }


    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(UnitOfWork));

        await _connectionLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_transaction == null) throw new InvalidOperationException("No active transaction to commit.");

            try
            {
                if (_transaction is DbTransaction dbTransaction)
                    await dbTransaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                else
                    _transaction.Commit();

                _logger.LogInformation("Transaction committed. Id: {TransactionId}", TransactionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Commit failed for transaction {TransactionId}. Rolling back.", TransactionId);
                await RollbackAsyncCore(cancellationToken).ConfigureAwait(false);
                throw;
            }
            finally
            {
                await ResetTransactionStateAsync().ConfigureAwait(false);
            }
        }
        finally
        {
            _connectionLock.Release();
        }
    }


    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        await _connectionLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await RollbackAsyncCore(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _connectionLock.Release();
        }
    }


    private async Task RollbackAsyncCore(CancellationToken cancellationToken)
    {
        if (_transaction == null) return;

        try
        {
            if (_transaction is DbTransaction dbTransaction)
                await dbTransaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            else
                _transaction.Rollback();

            _logger.LogInformation("Transaction rolled back. Id: {TransactionId}", TransactionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during rollback of transaction {TransactionId}. Connection may need recreation.", TransactionId);
            throw new InfrastructureException(ex, $"Rollback failed for transaction {TransactionId}. See inner exception.");
        }
        finally
        {
            await ResetTransactionStateAsync().ConfigureAwait(false);
        }
    }

    private async Task ResetTransactionStateAsync()
    {
        if (_transaction != null)
        {
            if (_transaction is IAsyncDisposable asyncDisposable)
                await asyncDisposable.DisposeAsync().ConfigureAwait(false);
            else
                _transaction.Dispose();
        }

        _transaction = null;
        _transactionId = null;
        _savepoints.Clear();
        _domainEvents.Clear();
    }

    // --------------------------------------------------------------
    //  آزادسازی زودهنگام Connection (بدون Dispose کردن کل UnitOfWork)
    // --------------------------------------------------------------
    public async Task ReleaseConnectionAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(UnitOfWork));

        await _connectionLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_transaction != null)
            {
                _logger.LogWarning(
                    "ReleaseConnectionAsync called while a transaction is still active (Id: {TransactionId}). Forcing rollback.",
                    TransactionId);

                await RollbackAsyncCore(cancellationToken).ConfigureAwait(false);
            }

            if (_connection != null)
            {
                if (_connection is IAsyncDisposable asyncDisposable)
                    await asyncDisposable.DisposeAsync().ConfigureAwait(false);
                else
                    _connection.Dispose();

                _connection = null;
                _logger.LogInformation("Connection explicitly released back to pool.");
            }
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    // --------------------------------------------------------------
    //  GetRepository Helpers
    // --------------------------------------------------------------

    public Task<TRepository> GetRepositoryAsync<TRepository>(CancellationToken ct = default) where TRepository : class
    {
        ct.ThrowIfCancellationRequested();

        var repositoryType = typeof(TRepository);

        if (_repositories.TryGetValue(repositoryType, out var existingRepository))
            return Task.FromResult((TRepository)existingRepository);


        var repository = _serviceProvider.GetService<TRepository>();

        if (repository == null)
            throw new InvalidOperationException($"Repository of type {repositoryType.Name} is not registered in the DI container.");

        _repositories[repositoryType] = repository;

        return Task.FromResult(repository);
    }

    public TRepository GetRepository<TRepository>() where TRepository : class
    {
        var repositoryType = typeof(TRepository);

        if (_repositories.TryGetValue(repositoryType, out var existingRepository))
            return (TRepository)existingRepository;

        var repository = _serviceProvider.GetService<TRepository>();


        if (repository == null)
            throw new InvalidOperationException($"Repository of type {repositoryType.Name} is not registered in the DI container.");

        _repositories[repositoryType] = repository;

        return repository;
    }



    // --------------------------------------------------------------
    //  Dispose
    // --------------------------------------------------------------
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore().ConfigureAwait(false);
        Dispose(false);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (_disposed) return;
        if (disposing)
        {
            _transaction?.Dispose();
            _connection?.Dispose();
            _connectionLock?.Dispose();
        }
        _disposed = true;
    }

    private async ValueTask DisposeAsyncCore()
    {
        if (_disposed) return;

        if (_transaction is IAsyncDisposable asyncTran)
            await asyncTran.DisposeAsync().ConfigureAwait(false);
        else
            _transaction?.Dispose();

        if (_connection is IAsyncDisposable asyncConn)
            await asyncConn.DisposeAsync().ConfigureAwait(false);
        else
            _connection?.Dispose();

        if (_connectionLock is IAsyncDisposable asyncLock)
            await asyncLock.DisposeAsync().ConfigureAwait(false);
        else
            _connectionLock?.Dispose();

        _disposed = true;
    }


}

