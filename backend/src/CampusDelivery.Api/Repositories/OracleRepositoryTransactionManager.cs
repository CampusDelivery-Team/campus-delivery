using CampusDelivery.Api.Persistence.Oracle;
using CampusDelivery.Api.Repositories.Interfaces;
using Oracle.ManagedDataAccess.Client;

namespace CampusDelivery.Api.Repositories;

public sealed class OracleRepositoryTransactionManager(OracleConnectionFactory connectionFactory)
    : IRepositoryTransactionManager
{
    public async Task<IRepositoryTransaction> BeginAsync(
        CancellationToken cancellationToken = default)
    {
        OracleConnection connection = connectionFactory.CreateConnection();
        try
        {
            await connection.OpenAsync(cancellationToken);
            OracleTransaction transaction =
                (OracleTransaction)await connection.BeginTransactionAsync(cancellationToken);
            return new OracleRepositoryTransaction(connection, transaction);
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }
}

internal sealed class OracleRepositoryTransaction(
    OracleConnection connection,
    OracleTransaction transaction) : IRepositoryTransaction
{
    private bool _completed;

    internal OracleConnection Connection { get; } = connection;

    internal OracleTransaction Transaction { get; } = transaction;

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        if (_completed)
        {
            return;
        }

        await Transaction.CommitAsync(cancellationToken);
        _completed = true;
    }

    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        if (_completed)
        {
            return;
        }

        await Transaction.RollbackAsync(cancellationToken);
        _completed = true;
    }

    public async ValueTask DisposeAsync()
    {
        if (!_completed)
        {
            try
            {
                await Transaction.RollbackAsync();
            }
            catch
            {
                // Preserve the original operation failure while still disposing resources.
            }
        }

        await Transaction.DisposeAsync();
        await Connection.DisposeAsync();
    }
}

internal static class RepositoryTransactionAccessor
{
    internal static (OracleConnection Connection, OracleTransaction Transaction) GetOracle(
        this IRepositoryTransaction transaction)
    {
        if (transaction is not OracleRepositoryTransaction oracleTransaction)
        {
            throw new ArgumentException("事务实例不是当前 Oracle 持久层创建的事务。", nameof(transaction));
        }

        return (oracleTransaction.Connection, oracleTransaction.Transaction);
    }
}
