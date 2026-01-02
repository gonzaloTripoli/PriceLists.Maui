using Microsoft.Maui.Controls;
using PriceLists.Maui.ViewModels;

namespace PriceLists.Maui.Views;

public partial class ListDetailPage : ContentPage
{
    private readonly ListDetailViewModel viewModel;

    public ListDetailPage(ListDetailViewModel viewModel)
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
