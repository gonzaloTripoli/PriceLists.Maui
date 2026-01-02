using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private string? statusMessage;

    [ObservableProperty]
    private ObservableCollection<PriceList> priceLists = new();
    [ObservableProperty]
    private PriceList? selectedPriceList;

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
    }


    partial void OnSelectedPriceListChanged(PriceList? value)
    {
        if (value is null) return;

        // Disparamos navegación (fire and forget, porque el partial no puede ser async)
        _ = OpenListAsync(value);

        // Deselecciona para permitir tocar el mismo item nuevamente
        SelectedPriceList = null;
    }

    public async Task LoadAsync()
    {
        if (IsBusy)
        {
            return;
        }

        try
        {
            IsBusy = true;
            StatusMessage = "Cargando listas...";

            var lists = await priceListRepository.GetAllAsync();
            PriceLists = new ObservableCollection<PriceList>(lists);

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
            await priceListService.ImportExcelAsNewListAsync(fileResult.FullPath, listName.Trim());
            StatusMessage = "Importación exitosa";

            await LoadAsync();
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

}
