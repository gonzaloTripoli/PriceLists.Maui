namespace PriceLists.Core.Models;

public class ImportPreview
{
    public required string SheetName { get; init; }

    public int HeaderRow { get; init; }

    public int CodeColumn { get; init; }

    public int DescriptionColumn { get; init; }

    public int PriceColumn { get; init; }

    public required IReadOnlyList<PriceItemPreviewRow> Rows { get; init; }
}
