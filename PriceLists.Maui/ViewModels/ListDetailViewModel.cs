using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
    private CancellationTokenSource? filterCancellationTokenSource;

    [ObservableProperty]
    private string? listName;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private string? statusMessage;

    [ObservableProperty]
    private string? searchText;

    [ObservableProperty]
    private bool isEmpty;

    [ObservableProperty]
    private int totalItemsCount;

    [ObservableProperty]
    private int filteredItemsCount;

    [ObservableProperty]
    private string? sectionLabel;

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
            TotalItemsCount = allItems.Count;
            SectionLabel = BuildSectionLabel();
            await ApplyFilterAsync();

            StatusMessage = string.Empty;
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
            UpdateEmptyState();
        }
    }

    partial void OnSearchTextChanged(string? value)
    {
        _ = DebounceFilterAsync();
    }

    partial void OnStatusMessageChanged(string? value)
    {
        UpdateEmptyState();
    }

    private async Task DebounceFilterAsync()
    {
        filterCancellationTokenSource?.Cancel();
        filterCancellationTokenSource?.Dispose();

        var tokenSource = new CancellationTokenSource();
        filterCancellationTokenSource = tokenSource;
        var token = tokenSource.Token;

        try
        {
            await Task.Delay(200, token);
            await ApplyFilterAsync(token);
        }
        catch (TaskCanceledException)
        {
            // Debounce canceled
        }
        finally
        {
            tokenSource.Dispose();
            if (filterCancellationTokenSource == tokenSource)
            {
                filterCancellationTokenSource = null;
            }
        }
    }

    private async Task ApplyFilterAsync(CancellationToken ct = default)
    {
        var query = SearchText?.Trim();
        IEnumerable<PriceItem> filtered = allItems;

        ct.ThrowIfCancellationRequested();

        if (!string.IsNullOrWhiteSpace(query))
        {
            filtered = filtered.Where(item =>
                (!string.IsNullOrEmpty(item.Code) && item.Code.Contains(query, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(item.Description) && item.Description.Contains(query, StringComparison.OrdinalIgnoreCase)));
        }

        var filteredList = filtered.ToList();
        ct.ThrowIfCancellationRequested();
        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            Items.Clear();
            foreach (var item in filteredList)
            {
                Items.Add(item);
            }

            FilteredItemsCount = Items.Count;
            UpdateEmptyState();
        });
    }

    private string? BuildSectionLabel()
    {
        var distinctSections = allItems
            .Select(x => x.SectionName)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return distinctSections.Count switch
        {
            0 => null,
            1 => $"Sección: {distinctSections[0]}",
            _ => $"Secciones: {distinctSections.Count}"
        };
    }

    private void UpdateEmptyState()
    {
        IsEmpty = !IsBusy && Items.Count == 0 && string.IsNullOrWhiteSpace(StatusMessage);
    }
}
