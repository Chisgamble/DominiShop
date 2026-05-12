using Microsoft.UI.Xaml.Controls;
using DominiShop.ViewModel;
using Microsoft.Extensions.DependencyInjection;

namespace DominiShop.View;

public sealed partial class SettingsPage : Page
{
    public SettingsViewModel ViewModel { get; }
    public SettingsPage()
    {
        ViewModel = App.Services.GetRequiredService<SettingsViewModel>();
        this.InitializeComponent();
    }
}