using PriceLists.Core.Models;

namespace PriceLists.Core.Abstractions;

public interface IPriceListService
{
    Task<PriceList> ImportExcelAsNewListAsync(string filePath, string listName, CancellationToken ct = default);
}
