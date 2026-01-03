using Microsoft.Maui.Controls;
using PriceLists.Maui.ViewModels;

namespace PriceLists.Maui.Views;

public partial class ListsPage : ContentPage
{
    private readonly ListsViewModel viewModel;

    private bool _loaded;

    public ListsPage(ListsViewModel viewModel)
    {
        InitializeComponent();
        this.viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_loaded) return;
        _loaded = true;

        await viewModel.LoadAsync();
    }
}
