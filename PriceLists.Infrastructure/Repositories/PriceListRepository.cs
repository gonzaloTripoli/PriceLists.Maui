using Microsoft.EntityFrameworkCore;
using PriceLists.Core.Abstractions;
using PriceLists.Core.Models;
using PriceLists.Infrastructure.Persistence;

namespace PriceLists.Infrastructure.Repositories;

public class PriceListRepository : IPriceListRepository
{
    private readonly AppDbContext dbContext;

    public PriceListRepository(AppDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<List<PriceList>> GetAllAsync(CancellationToken ct = default)
    {
        return await dbContext.PriceLists
            .OrderByDescending(x => x.ImportedAtUtc)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<PriceList?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await dbContext.PriceLists
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct);
    }

    public async Task<List<PriceItem>> GetItemsAsync(Guid priceListId, CancellationToken ct = default)
    {
        return await dbContext.PriceItems
            .Where(x => x.PriceListId == priceListId)
            .OrderBy(x => x.Description)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<Guid> CreateListWithItemsAsync(PriceList list, IEnumerable<PriceItem> items, CancellationToken ct = default)
    {
        using var transaction = await dbContext.Database.BeginTransactionAsync(ct);

        list.Id = list.Id == Guid.Empty ? Guid.NewGuid() : list.Id;
        await dbContext.PriceLists.AddAsync(list, ct);

        var itemsToAdd = items.Select(item =>
        {
            item.Id = item.Id == Guid.Empty ? Guid.NewGuid() : item.Id;
            item.PriceListId = list.Id;
            return item;
        }).ToList();

        await dbContext.PriceItems.AddRangeAsync(itemsToAdd, ct);
        await dbContext.SaveChangesAsync(ct);

        await transaction.CommitAsync(ct);

        return list.Id;
    }
}
