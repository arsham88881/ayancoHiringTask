using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Interfaces.Services;

public interface IFileReaderService
{
    Task<IEnumerable<T>> ReadFromFileAsync<T>(string folderPath, string fileName);
    Task<T> ReadSingleAsync<T>(string folderPath, string fileName);
}