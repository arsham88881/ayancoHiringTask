using Domain.Interfaces.Services;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace Infrastructure.DataIo.Services;


public class FileReaderService : IFileReaderService
{
    public async Task<IEnumerable<T>> ReadFromFileAsync<T>(string folderPath, string fileName)
    {
        var filePath = Path.Combine(folderPath, fileName);

        if (!File.Exists(filePath))
            return Enumerable.Empty<T>();

        var jsonContent = await File.ReadAllTextAsync(filePath);

        // اگر محتوا خالی بود
        if (string.IsNullOrWhiteSpace(jsonContent))
            return Enumerable.Empty<T>();

        try
        {
            // اگر فایل با آرایه JSON ذخیره شده
            if (jsonContent.TrimStart().StartsWith("["))
            {
                return JsonSerializer.Deserialize<IEnumerable<T>>(jsonContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? Enumerable.Empty<T>();
            }
            // اگر فقط یک object ذخیره شده
            else
            {
                var singleObject = JsonSerializer.Deserialize<T>(jsonContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                return singleObject != null ? new[] { singleObject } : Enumerable.Empty<T>();
            }
        }
        catch (JsonException)
        {
            // در صورت خطا در فرمت JSON، آرایه خالی برگردون
            return Enumerable.Empty<T>();
        }
    }

    public async Task<T> ReadSingleAsync<T>(string folderPath, string fileName)
    {
        var items = await ReadFromFileAsync<T>(folderPath, fileName);
        return items.FirstOrDefault();
    }
}