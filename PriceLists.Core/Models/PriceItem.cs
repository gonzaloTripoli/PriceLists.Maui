namespace PriceLists.Core.Models;

public class PriceItem
{
    public Guid Id { get; set; }

    public Guid PriceListId { get; set; }

    public string? Code { get; set; }

    public required string Description { get; set; }

    public decimal UnitPrice { get; set; }

    public string? SectionName { get; set; }

    public PriceList? PriceList { get; set; }
}
