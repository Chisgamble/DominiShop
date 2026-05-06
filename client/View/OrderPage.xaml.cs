using DominiShop.ViewModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace DominiShop.View;

public sealed partial class OrderPage : Page
{
    public OrderViewModel ViewModel { get; }

    public OrderPage()
    {
        InitializeComponent();
        ViewModel = App.Services.GetRequiredService<OrderViewModel>();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        await ViewModel.LoadOrdersAsync();
    }
}