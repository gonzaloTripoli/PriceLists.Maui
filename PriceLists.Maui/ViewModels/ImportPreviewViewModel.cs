using CommunityToolkit.Mvvm.ComponentModel;
using PriceLists.Core.Models;
using PriceLists.Maui.Services;

namespace PriceLists.Maui.ViewModels;

public partial class ImportPreviewViewModel : ObservableObject
{
    [ObservableProperty]
    private string sheetName = string.Empty;

    [ObservableProperty]
    private int headerRow;

    [ObservableProperty]
    private int codeColumn;

    [ObservableProperty]
    private int descriptionColumn;

    [ObservableProperty]
    private int priceColumn;

    public IReadOnlyList<PriceItemPreviewRow> Rows { get; }

    public ImportPreviewViewModel(PreviewStore previewStore)
    {
        var preview = previewStore.TakePreview() ?? throw new InvalidOperationException("No hay datos de preview disponibles.");
        SheetName = preview.SheetName;
        HeaderRow = preview.HeaderRow;
        CodeColumn = preview.CodeColumn;
        DescriptionColumn = preview.DescriptionColumn;
        PriceColumn = preview.PriceColumn;
        Rows = preview.Rows;
    }
}
