using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using PriceLists.Core.Abstractions;
using PriceLists.Core.Models;

namespace PriceLists.Maui.ViewModels;

public partial class ListDetailViewModel : ObservableObject, IQueryAttributable
{
    private readonly IPriceListRepository priceListRepository;
    private Guid priceListId;
    private List<PriceItemRowViewModel> allItems = new();
    private CancellationTokenSource? filterCancellationTokenSource;
    private CancellationTokenSource? saveStatusTokenSource;
    private readonly CultureInfo currencyCulture = new("es-AR");

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

    [ObservableProperty]
    private bool isSaving;

    [ObservableProperty]
    private string? saveStatusMessage;

    public ObservableCollection<PriceItemRowViewModel> Items { get; } = new();

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
            saveStatusTokenSource?.Cancel();
            saveStatusTokenSource?.Dispose();
            saveStatusTokenSource = null;
            SaveStatusMessage = string.Empty;
            IsSaving = false;

            IsBusy = true;
            StatusMessage = "Cargando productos...";

            var list = await priceListRepository.GetByIdAsync(priceListId);
            ListName = list?.Name ?? "Lista";

            var items = await priceListRepository.GetItemsAsync(priceListId);
            allItems = items
                .Select(MapToRowViewModel)
                .ToList();

            TotalItemsCount = allItems.Count;
            SectionLabel = BuildSectionLabel(allItems);
            await ApplyFilterAsync();

            StatusMessage = string.Empty;
            SaveStatusMessage = string.Empty;
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

    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadAsync();
    }

    [RelayCommand]
    private async Task EditPriceAsync(PriceItemRowViewModel? item)
    {
        if (item is null || IsBusy || IsSaving)
        {
            return;
        }

        if (Shell.Current is null)
        {
            StatusMessage = "No se pudo abrir el diálogo de edición.";
            return;
        }

        var initialValue = item.UnitPrice.ToString("N2", currencyCulture);
        var promptResult = await Shell.Current.DisplayPromptAsync(
            "Editar precio",
            $"Ingresa el nuevo precio para \"{item.Description}\"",
            accept: "Guardar",
            cancel: "Cancelar",
            keyboard: Keyboard.Numeric,
            initialValue: initialValue);

        if (promptResult is null)
        {
            await ShowSaveStatusAsync("Edición cancelada");
            return;
        }

        if (!TryParsePrice(promptResult, out var parsedPrice, out var validationMessage))
        {
            StatusMessage = validationMessage;
            await ShowSaveStatusAsync("Precio inválido");
            return;
        }

        if (parsedPrice == item.UnitPrice)
        {
            await ShowSaveStatusAsync("Sin cambios");
            return;
        }

        try
        {
            IsSaving = true;
            await ShowSaveStatusAsync("Guardando...", autoClear: false);

            await priceListRepository.UpdateItemPriceAsync(item.Id, parsedPrice);
            item.UpdateUnitPrice(parsedPrice);

            StatusMessage = string.Empty;
            await ShowSaveStatusAsync("Precio guardado");
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
            await ShowSaveStatusAsync("Error al guardar", autoClear: false);
        }
        finally
        {
            IsSaving = false;
        }
    }

    private async Task ApplyFilterAsync(CancellationToken ct = default)
    {
        var query = SearchText?.Trim();
        IEnumerable<PriceItemRowViewModel> filtered = allItems;

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

    private bool TryParsePrice(string? input, out decimal price, out string validationMessage)
    {
        price = 0;
        validationMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(input))
        {
            validationMessage = "Ingresa un valor numérico.";
            return false;
        }

        var sanitized = input
            .Replace("ARS", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("$", string.Empty)
            .Trim();

        var cultures = new[]
        {
            currencyCulture,
            CultureInfo.InvariantCulture
        };

        foreach (var culture in cultures)
        {
            if (decimal.TryParse(sanitized, NumberStyles.Number | NumberStyles.AllowCurrencySymbol, culture, out price))
            {
                if (price < 0)
                {
                    validationMessage = "El precio no puede ser negativo.";
                    return false;
                }

                return true;
            }
        }

        var normalized = sanitized.Replace(",", ".").Replace(" ", string.Empty);
        if (decimal.TryParse(normalized, NumberStyles.Number | NumberStyles.AllowCurrencySymbol, CultureInfo.InvariantCulture, out price))
        {
            if (price < 0)
            {
                validationMessage = "El precio no puede ser negativo.";
                return false;
            }

            return true;
        }

        validationMessage = "Ingresa un precio válido (usa , o . como separador decimal).";
        return false;
    }

    private async Task ShowSaveStatusAsync(string message, bool autoClear = true, int delayMs = 2000)
    {
        saveStatusTokenSource?.Cancel();
        saveStatusTokenSource?.Dispose();
        saveStatusTokenSource = null;

        SaveStatusMessage = message;

        if (!autoClear)
        {
            return;
        }

        var cts = new CancellationTokenSource();
        saveStatusTokenSource = cts;

        try
        {
            await Task.Delay(delayMs, cts.Token);
            await MainThread.InvokeOnMainThreadAsync(() => SaveStatusMessage = string.Empty);
        }
        catch (TaskCanceledException)
        {
            // ignored
        }
        finally
        {
            cts.Dispose();
            if (ReferenceEquals(saveStatusTokenSource, cts))
            {
                saveStatusTokenSource = null;
            }
        }
    }

    private static PriceItemRowViewModel MapToRowViewModel(PriceItem item)
    {
        return new PriceItemRowViewModel
        {
            Id = item.Id,
            PriceListId = item.PriceListId,
            Code = item.Code,
            Description = item.Description,
            UnitPrice = item.UnitPrice,
            SectionName = item.SectionName
        };
    }

    private static string? BuildSectionLabel(IEnumerable<PriceItemRowViewModel> items)
    {
        var distinctSections = items
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
