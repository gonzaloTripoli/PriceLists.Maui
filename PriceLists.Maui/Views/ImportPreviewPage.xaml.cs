using PriceLists.Maui.ViewModels;

namespace PriceLists.Maui.Views;

public partial class ImportPreviewPage : ContentPage
{
    public ImportPreviewPage(ImportPreviewViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        if (Shell.Current is not null)
        {
            await Shell.Current.GoToAsync("..");
        }
        else if (Navigation.NavigationStack.Count > 0)
        {
            await Navigation.PopAsync();
        }
    }
}
