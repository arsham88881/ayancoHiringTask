using Domain.Interfaces.Services;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace Infrastructure.DataIo.Services;

public class FileStorageService : IFileStorageService
{
    public async Task AppendToFileAsync<T>(string folderPath, string fileName, T data)
    {
        if (!Directory.Exists(folderPath))
            Directory.CreateDirectory(folderPath);

        var filePath = Path.Combine(folderPath, fileName);
        var jsonString = JsonSerializer.Serialize(data, new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });

        // اگر فایل وجود داره، اول جداکننده اضافه کن
        if (File.Exists(filePath) && new FileInfo(filePath).Length > 0)
            await File.AppendAllTextAsync(filePath, $",{Environment.NewLine}");
        else
            await File.WriteAllTextAsync(filePath, $"[{Environment.NewLine}");


        await File.AppendAllTextAsync(filePath, jsonString);
    }

    public async Task WriteAllAsync<T>(string folderPath, string fileName, IEnumerable<T> dataList)
    {
        if (!Directory.Exists(folderPath))
            Directory.CreateDirectory(folderPath);

        var filePath = Path.Combine(folderPath, fileName);
        var jsonString = JsonSerializer.Serialize(dataList, new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });

        await File.WriteAllTextAsync(filePath, jsonString);
    }
}
