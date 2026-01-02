using System.IO;
using PriceLists.Core.Abstractions;
using PriceLists.Core.Models;

namespace PriceLists.Infrastructure.Services;

public class PriceListService : IPriceListService
{
    private readonly IExcelImportService excelImportService;
    private readonly IPriceListRepository priceListRepository;

    public PriceListService(IExcelImportService excelImportService, IPriceListRepository priceListRepository)
    {
        this.excelImportService = excelImportService;
        this.priceListRepository = priceListRepository;
    }

    public async Task<Guid> ImportExcelAsNewListAsync(string filePath, string listName, CancellationToken ct = default)
    {
        var preview = await excelImportService.ImportAsync(filePath, maxRows: null, ct);

        var priceList = new PriceList
        {
            Name = listName,
            SourceFileName = Path.GetFileName(filePath),
            ImportedAtUtc = DateTime.UtcNow
        };

        var priceItems = MapRowsToItems(preview.Rows);

        return await priceListRepository.CreateListWithItemsAsync(priceList, priceItems, ct);
    }

    private static IEnumerable<PriceItem> MapRowsToItems(IReadOnlyList<PriceItemPreviewRow> rows)
    {
        var mapped = new List<PriceItem>();
        string? currentSection = null;

        foreach (var row in rows)
        {
            var code = row.Code?.Trim();
            var description = row.Description?.Trim();
            var price = row.Price;

            if (string.IsNullOrWhiteSpace(code) && !string.IsNullOrWhiteSpace(description) && (price is null || price == 0))
            {
                currentSection = description;
                continue;
            }

            if (string.IsNullOrWhiteSpace(description))
            {
                continue;
            }

            mapped.Add(new PriceItem
            {
                Description = description!,
                Code = string.IsNullOrWhiteSpace(code) ? null : code,
                UnitPrice = price ?? 0,
                SectionName = currentSection
            });
        }

        return mapped;
    }
}
