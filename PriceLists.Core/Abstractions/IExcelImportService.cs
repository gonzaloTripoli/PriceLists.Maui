using PriceLists.Core.Models;

namespace PriceLists.Core.Abstractions;

public interface IExcelImportService
{
    Task<ImportPreview> ImportAsync(string filePath, CancellationToken ct = default);
}
