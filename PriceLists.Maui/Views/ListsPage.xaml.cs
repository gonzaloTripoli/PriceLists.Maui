using PriceLists.Maui.ViewModels;

namespace PriceLists.Maui.Views;

public partial class ListsPage : ContentPage
{
    public ListsPage(ListsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
