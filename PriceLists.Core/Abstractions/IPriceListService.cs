namespace PriceLists.Core.Abstractions;

public interface IPriceListService
{
    Task<Guid> ImportExcelAsNewListAsync(string filePath, string listName, CancellationToken ct = default);
}
