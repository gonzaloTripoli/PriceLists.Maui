using PriceLists.Core.Models;

namespace PriceLists.Core.Abstractions;

public interface IPriceListRepository
{
    Task<List<PriceList>> GetAllAsync(CancellationToken ct = default);

    Task<List<PriceListSummary>> GetAllWithCountsAsync(CancellationToken ct = default);

    Task<PriceList?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<List<PriceItem>> GetItemsAsync(Guid priceListId, CancellationToken ct = default);

    Task<PriceItem?> GetItemByIdAsync(Guid id, CancellationToken ct = default);

    Task UpdateItemPriceAsync(Guid itemId, decimal newPrice, CancellationToken ct = default);

    Task<Guid> CreateListWithItemsAsync(PriceList list, IEnumerable<PriceItem> items, CancellationToken ct = default);
}
