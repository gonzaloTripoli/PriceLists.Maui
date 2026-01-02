using Microsoft.Maui.Controls;
using PriceLists.Maui.ViewModels;

namespace PriceLists.Maui.Views;

public partial class ListsPage : ContentPage
{
    private readonly ListsViewModel viewModel;

    public ListsPage(ListsViewModel viewModel)
    {
        InitializeComponent();
        this.viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await viewModel.LoadAsync();
    }
}
