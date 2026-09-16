using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace Infrastructure.Persistence.ValueObjects;

internal static class DapperCommandFactory
{
    public static CommandDefinition Create(
        string commandText,
        object? parameters,
        IDbTransaction? transaction,
        int? commandTimeout,
        CommandType commandType,
        CancellationToken cancellationToken)
        => new(commandText, parameters, transaction, commandTimeout, commandType, cancellationToken: cancellationToken);
}

