using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using PriceLists.Core.Abstractions;
using PriceLists.Maui.Services;
using PriceLists.Maui.Views;

namespace PriceLists.Maui.ViewModels;

public partial class ListsViewModel : ObservableObject
{
    private readonly IExcelImportService excelImportService;
    private readonly PreviewStore previewStore;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private string? statusMessage;

    public ListsViewModel(IExcelImportService excelImportService, PreviewStore previewStore)
    {
        this.excelImportService = excelImportService;
        this.previewStore = previewStore;
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

            StatusMessage = "Importando...";
            var preview = await excelImportService.ImportAsync(fileResult.FullPath);
            previewStore.SetPreview(preview);
            StatusMessage = string.Empty;

            if (Shell.Current is not null)
            {
                await Shell.Current.GoToAsync(nameof(ImportPreviewPage));
            }
            else
            {
                StatusMessage = "No se pudo navegar al preview";
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
}
