using Domain.Interfaces.Services;
using Domain.Models.Audit;
using System;
using System.Collections.Generic;
using System.Text;

namespace Infrastructure.Security.Services;

public class ExceptionLogService : IExceptionLogService
{
    private readonly IFileStorageService _fileStorageService;
    private readonly IFileReaderService _fileReaderService;
    private readonly string _basePath;

    public ExceptionLogService(
        IFileStorageService fileStorageService,
        IFileReaderService fileReaderService,
        string basePath = "Logs")
    {
        _fileStorageService = fileStorageService;
        _fileReaderService = fileReaderService;
        _basePath = basePath;
    }

    public async Task LogExceptionAsync(string folderName, ExceptionLogEntry entry)
    {
        var folderPath = Path.Combine(_basePath, folderName);
        var fileName = $"exceptions_{DateTime.Now:yyyyMMdd}.json";

        await _fileStorageService.AppendToFileAsync(folderPath, fileName, entry);
    }

    public async Task<IEnumerable<ExceptionLogEntry>> GetExceptionsAsync(string folderName)
    {
        var folderPath = Path.Combine(_basePath, folderName);

        if (!Directory.Exists(folderPath))
            return Enumerable.Empty<ExceptionLogEntry>();

        var jsonFiles = Directory.GetFiles(folderPath, "exceptions_*.json");
        var allExceptions = new List<ExceptionLogEntry>();

        foreach (var file in jsonFiles)
        {
            var exceptions = await _fileReaderService.ReadFromFileAsync<ExceptionLogEntry>(folderPath, Path.GetFileName(file));
            allExceptions.AddRange(exceptions);
        }

        return allExceptions.OrderByDescending(e => e.LoggedAt);
    }

    public async Task CloseCurrentBatchAsync(string folderName)
    {
        var folderPath = Path.Combine(_basePath, folderName);
        var todayFileName = $"exceptions_{DateTime.Now:yyyyMMdd}.json";
        var filePath = Path.Combine(folderPath, todayFileName);

        if (File.Exists(filePath))
            await File.AppendAllTextAsync(filePath, $"{Environment.NewLine}]");

    }
}