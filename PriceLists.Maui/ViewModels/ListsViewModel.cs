using System.Collections.ObjectModel;
using System.IO;
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

    public ObservableCollection<PriceList> PriceLists { get; } = new();

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private bool isRefreshing;

    [ObservableProperty]
    private string? statusMessage;

    [ObservableProperty]
    private PriceList? selectedPriceList;

    [ObservableProperty]
    private bool isEmpty;

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


    partial void OnSelectedPriceListChanged(PriceList? value)
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

            var lists = await priceListRepository.GetAllAsync();

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                PriceLists.Clear();
                foreach (var list in lists)
                {
                    PriceLists.Add(list);
                }
            });

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
    private async Task OpenListAsync(PriceList selected)
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

        MainThread.BeginInvokeOnMainThread(() =>
        {
            var existingIndex = -1;
            for (var i = 0; i < PriceLists.Count; i++)
            {
                if (PriceLists[i].Id == newList.Id)
                {
                    existingIndex = i;
                    break;
                }
            }

            if (existingIndex >= 0)
            {
                PriceLists.RemoveAt(existingIndex);
            }

            var insertIndex = 0;
            while (insertIndex < PriceLists.Count && PriceLists[insertIndex].ImportedAtUtc > newList.ImportedAtUtc)
            {
                insertIndex++;
            }

            PriceLists.Insert(insertIndex, newList);
            UpdateStateFlags();
        });
    }

    private void UpdateStateFlags()
    {
        IsEmpty = !IsBusy && PriceLists.Count == 0 && string.IsNullOrWhiteSpace(StatusMessage);
    }
}
