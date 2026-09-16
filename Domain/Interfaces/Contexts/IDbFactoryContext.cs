using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Interfaces.Contexts;

public interface IDbFactoryContext
{
    SqlConnection CreateConnection();
}