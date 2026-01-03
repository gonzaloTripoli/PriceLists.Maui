using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using PriceLists.Core.Abstractions;
using PriceLists.Core.Models;
using PriceLists.Maui.Services;
using PriceLists.Maui.Views;

namespace PriceLists.Maui.ViewModels;

public partial class ListsViewModel : ObservableObject
{
    private readonly IPriceListRepository priceListRepository;
    private readonly IPriceListService priceListService;
    private readonly IExcelImportService excelImportService;
    private readonly PreviewStore previewStore;
    private List<PriceListSummary> allLists = new();
    private CancellationTokenSource? filterCancellationTokenSource;

    public ObservableCollection<PriceListSummary> PriceLists { get; } = new();

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private bool isRefreshing;

    [ObservableProperty]
    private string? statusMessage;

    [ObservableProperty]
    private PriceListSummary? selectedPriceList;

    [ObservableProperty]
    private bool isEmpty;

    [ObservableProperty]
    private string? searchText;

    [ObservableProperty]
    private int totalListsCount;

    [ObservableProperty]
    private int filteredListsCount;

    public ListsViewModel(
        IPriceListRepository priceListRepository,
        IPriceListService priceListService,
        IExcelImportService excelImportService,
        PreviewStore previewStore)
    {
        this.priceListRepository = priceListRepository;
        this.priceListService = priceListService;
        this.excelImportService = excelImportService;
        this.previewStore = previewStore;

        PriceLists.CollectionChanged += (_, _) => UpdateStateFlags();
    }


    partial void OnSelectedPriceListChanged(PriceListSummary? value)
    {
        if (value is null) return;

        // Disparamos navegación (fire and forget, porque el partial no puede ser async)
        _ = OpenListAsync(value);

        // Deselecciona para permitir tocar el mismo item nuevamente
        SelectedPriceList = null;
    }

    partial void OnStatusMessageChanged(string? value)
    {
        UpdateStateFlags();
    }

    partial void OnSearchTextChanged(string? value)
    {
        _ = DebounceFilterAsync();
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
            // Ignored
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

    public async Task LoadAsync()
    {
        if (IsBusy)
        {
            IsRefreshing = false;
            return;
        }

        try
        {
            IsBusy = true;
            IsRefreshing = true;
            StatusMessage = "Cargando listas...";

            allLists = await priceListRepository.GetAllWithCountsAsync();
            TotalListsCount = allLists.Count;
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
            IsRefreshing = false;
            UpdateStateFlags();
        }
    }

    [RelayCommand]
    private async Task ImportExcelAsync()
    {
        if (IsBusy)
        {
            return;
        }

        try
        {
            IsBusy = true;
            StatusMessage = "Seleccionando archivo...";

            var fileResult = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Selecciona un Excel",
                FileTypes = GetExcelFileType()
            });

            if (fileResult is null)
            {
                StatusMessage = "Importación cancelada";
                return;
            }

            var defaultName = Path.GetFileNameWithoutExtension(fileResult.FileName);
            var listName = await Shell.Current.DisplayPromptAsync("Nombre de lista", "Ingresa un nombre para la lista", initialValue: defaultName, maxLength: 200);
            if (string.IsNullOrWhiteSpace(listName))
            {
                StatusMessage = "Nombre de lista requerido";
                return;
            }

            StatusMessage = "Importando...";
            var createdList = await priceListService.ImportExcelAsNewListAsync(fileResult.FullPath, listName.Trim());
            StatusMessage = "Importación exitosa";

            AddOrUpdateList(createdList);
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

    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadAsync();
    }

    [RelayCommand]
    private async Task PreviewExcelAsync()
    {
        if (IsBusy)
        {
            return;
        }

        try
        {
            IsBusy = true;
            StatusMessage = "Seleccionando archivo...";

            var fileResult = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Selecciona un Excel",
                FileTypes = GetExcelFileType()
            });

            if (fileResult is null)
            {
                StatusMessage = "Preview cancelado";
                return;
            }

            StatusMessage = "Generando preview...";
            var preview = await excelImportService.ImportAsync(fileResult.FullPath, maxRows: 20);
            previewStore.SetPreview(preview);
            StatusMessage = string.Empty;

            if (Shell.Current is not null)
            {
                await Shell.Current.GoToAsync(nameof(ImportPreviewPage));
            }
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

    private async Task ApplyFilterAsync(CancellationToken ct = default)
    {
        var query = SearchText?.Trim();
        IEnumerable<PriceListSummary> filtered = allLists;

        ct.ThrowIfCancellationRequested();

        if (!string.IsNullOrWhiteSpace(query))
        {
            filtered = filtered.Where(list =>
                (!string.IsNullOrWhiteSpace(list.Name) && list.Name.Contains(query, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrWhiteSpace(list.SourceFileName) && list.SourceFileName.Contains(query, StringComparison.OrdinalIgnoreCase)));
        }

        var filteredLists = filtered
            .OrderByDescending(x => x.ImportedAtUtc)
            .ToList();

        ct.ThrowIfCancellationRequested();

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            PriceLists.Clear();
            foreach (var list in filteredLists)
            {
                PriceLists.Add(list);
            }

            FilteredListsCount = PriceLists.Count;
            UpdateStateFlags();
        });
    }

    private static FilePickerFileType GetExcelFileType()
    {
        return new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
        {
            { DevicePlatform.Android, new[] { "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "application/vnd.ms-excel" } },
            { DevicePlatform.iOS, new[] { "org.openxmlformats.spreadsheetml.sheet" } },
            { DevicePlatform.MacCatalyst, new[] { "org.openxmlformats.spreadsheetml.sheet" } },
            { DevicePlatform.WinUI, new[] { ".xlsx" } },
        });
    }

    [RelayCommand]
    private async Task OpenListAsync(PriceListSummary selected)
    {
        if (selected is null || IsBusy) return;

        try
        {
            IsBusy = true;

            if (Shell.Current is null)
            {
                StatusMessage = "No se pudo navegar a la lista";
                return;
            }

            var parameters = new Dictionary<string, object>
            {
                { "priceListId", selected.Id }
            };

            await Shell.Current.GoToAsync(nameof(ListDetailPage), parameters);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void AddOrUpdateList(PriceList? newList)
    {
        if (newList is null)
        {
            return;
        }

        var summary = ToSummary(newList);
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            UpsertSummary(allLists, summary);
            TotalListsCount = allLists.Count;
            await ApplyFilterAsync();
        });
    }

    private static void UpsertSummary(ICollection<PriceListSummary> target, PriceListSummary summary)
    {
        var existing = target.FirstOrDefault(x => x.Id == summary.Id);
        if (existing is not null)
        {
            target.Remove(existing);
        }

        if (target is List<PriceListSummary> list)
        {
            var insertIndex = list.FindIndex(x => x.ImportedAtUtc <= summary.ImportedAtUtc);
            if (insertIndex < 0)
            {
                list.Add(summary);
            }
            else
            {
                list.Insert(insertIndex, summary);
            }
        }
        else
        {
            target.Add(summary);
        }
    }

    private static PriceListSummary ToSummary(PriceList list)
    {
        return new PriceListSummary
        {
            Id = list.Id,
            Name = list.Name,
            SourceFileName = list.SourceFileName,
            ImportedAtUtc = list.ImportedAtUtc,
            ItemsCount = list.Items?.Count ?? 0
        };
    }

    private void UpdateStateFlags()
    {
        IsEmpty = !IsBusy && PriceLists.Count == 0 && string.IsNullOrWhiteSpace(StatusMessage);
    }
}
