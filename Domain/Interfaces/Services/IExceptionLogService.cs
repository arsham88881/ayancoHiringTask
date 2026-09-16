using Domain.Models.Audit;
using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Interfaces.Services;

public interface IExceptionLogService
{
    Task LogExceptionAsync(string folderName, ExceptionLogEntry entry);
    Task<IEnumerable<ExceptionLogEntry>> GetExceptionsAsync(string folderName);
    Task CloseCurrentBatchAsync(string folderName);
}