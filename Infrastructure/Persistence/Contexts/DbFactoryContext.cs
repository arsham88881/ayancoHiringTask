using Domain.Interfaces.Contexts;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace Infrastructure.Persistence.Contexts;

public class DbFactoryContext : IDbFactoryContext
{
    private readonly string connectionString;

    public DbFactoryContext(IConfiguration configuration)
    {
        var rawConnectionString = configuration.GetConnectionString("MainDb");
        if (string.IsNullOrEmpty(rawConnectionString))
            throw new InvalidOperationException("Connection string is not available.");

        var builder = new SqlConnectionStringBuilder(rawConnectionString)
        {
            // --- Pool sizing ---
            MinPoolSize = 10,
            MaxPoolSize = 200,
            LoadBalanceTimeout = 30,
            ConnectTimeout = 15,
            Pooling = true,
            ApplicationName = "AyancoTask.Backend",
        };

        connectionString = builder.ConnectionString;
    }

    public SqlConnection CreateConnection()
    {
        return new SqlConnection(connectionString);
    }
}
