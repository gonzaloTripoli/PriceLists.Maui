using PriceLists.Core.Models;

namespace PriceLists.Core.Abstractions;

public interface IExcelImportService
{
    Task<ImportPreview> ImportAsync(string filePath, int? maxRows = 20, CancellationToken ct = default);
}
