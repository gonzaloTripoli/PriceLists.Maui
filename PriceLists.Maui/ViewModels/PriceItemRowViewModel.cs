using CommunityToolkit.Mvvm.ComponentModel;

namespace PriceLists.Maui.ViewModels;

public partial class PriceItemRowViewModel : ObservableObject
{
    public Guid Id { get; init; }

    public Guid PriceListId { get; init; }

    public string? Code { get; init; }

    public required string Description { get; init; }

    [ObservableProperty]
    private decimal unitPrice;

    public string? SectionName { get; init; }

    public void UpdateUnitPrice(decimal newPrice)
    {
        UnitPrice = newPrice;
    }
}
