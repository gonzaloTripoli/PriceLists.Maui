using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using PriceLists.Core.Abstractions;
using PriceLists.Core.Models;

namespace PriceLists.Maui.ViewModels;

public partial class ListDetailViewModel : ObservableObject, IQueryAttributable
{
    private readonly IPriceListRepository priceListRepository;
    private Guid priceListId;
    private List<PriceItem> allItems = new();

    [ObservableProperty]
    private string? listName;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private string? statusMessage;

    [ObservableProperty]
    private string? searchText;

    public ObservableCollection<PriceItem> Items { get; } = new();

    public ListDetailViewModel(IPriceListRepository priceListRepository)
    {
        this.priceListRepository = priceListRepository;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("priceListId", out var listIdObj) &&
            Guid.TryParse(listIdObj?.ToString(), out var id))
        {
            priceListId = id;
            MainThread.BeginInvokeOnMainThread(async () => await LoadAsync());
        }
    }

    public async Task LoadAsync()
    {
        if (priceListId == Guid.Empty || IsBusy)
        {
            return;
        }

        try
        {
            IsBusy = true;
            StatusMessage = "Cargando productos...";

            var list = await priceListRepository.GetByIdAsync(priceListId);
            ListName = list?.Name ?? "Lista";

            allItems = await priceListRepository.GetItemsAsync(priceListId);
            ApplyFilter();

            StatusMessage = string.Empty;
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    partial void OnSearchTextChanged(string? value)
    {
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        var query = SearchText?.Trim();
        IEnumerable<PriceItem> filtered = allItems;

        if (!string.IsNullOrWhiteSpace(query))
        {
            filtered = filtered.Where(item =>
                (!string.IsNullOrEmpty(item.Code) && item.Code.Contains(query, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(item.Description) && item.Description.Contains(query, StringComparison.OrdinalIgnoreCase)));
        }

        Items.Clear();
        foreach (var item in filtered)
        {
            Items.Add(item);
        }
    }
}
