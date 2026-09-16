using Domain.Models.Shared;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Exceptions;

public class BusinessException<OUTPUT> : Exception
{
    public ApiResponse<object> Result { get; private set; }

    public BusinessException(ApiResponse<object> result)
    {
        Result = result;
    }
}

public class BusinessException : Exception
{
    public ApiResponse Result { get; private set; }

    public BusinessException(ApiResponse result)
    {
        Result = result;
    }
}

