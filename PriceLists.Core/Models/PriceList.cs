namespace PriceLists.Core.Models;

public class PriceList
{
    public Guid Id { get; set; }

    public required string Name { get; set; }

    public string? SourceFileName { get; set; }

    public DateTime ImportedAtUtc { get; set; }

    public ICollection<PriceItem> Items { get; set; } = new List<PriceItem>();
}
