using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Interfaces.Services;

public interface IFileStorageService
{
    Task AppendToFileAsync<T>(string folderPath, string fileName, T data);
    Task WriteAllAsync<T>(string folderPath, string fileName, IEnumerable<T> dataList);
}