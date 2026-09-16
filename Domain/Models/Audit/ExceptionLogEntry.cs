using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Models.Audit;

public class ExceptionLogEntry
{
    public object? Object { get; set; }
    public ExceptionDetail? Exception { get; set; }
    public DateTime LoggedAt { get; set; } = DateTime.UtcNow;
}

public class ExceptionDetail
{
    public string? ExceptionType { get; set; }
    public string? StackTrace { get; set; }
    public string? InnerException { get; set; }
}

