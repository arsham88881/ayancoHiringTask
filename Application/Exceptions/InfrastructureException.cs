using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Exceptions;

public class InfrastructureException : Exception
{
    public InfrastructureException() { }
    public InfrastructureException(string? message)
     : base(message)
    {

    }
    public InfrastructureException(Exception innerException, string? message)
     : base(message, innerException)
    {

    }
}
