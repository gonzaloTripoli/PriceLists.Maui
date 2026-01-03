namespace PriceLists.Core.Models;

public class PriceListSummary
{
    public Guid Id { get; set; }

    public required string Name { get; set; }

    public string? SourceFileName { get; set; }

    public DateTime ImportedAtUtc { get; set; }

    public int ItemsCount { get; set; }
}
