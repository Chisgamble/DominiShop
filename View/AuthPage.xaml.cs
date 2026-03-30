using DominiShop.Service;
using DominiShop.ViewModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using System;

namespace DominiShop.View;

public sealed partial class AuthPage : Page
{
    public AuthViewModel ViewModel { get; } =
        App.Services.GetRequiredService<AuthViewModel>();

    public AuthPage()
    {
        InitializeComponent();
    }

    private void LoginPasswordBox_PasswordChanged(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        => ViewModel.LoginPassword = ((PasswordBox)sender).Password;

    private void SignUpPasswordBox_PasswordChanged(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        => ViewModel.SignUpPassword = ((PasswordBox)sender).Password;

    private void SignUpConfirmPasswordBox_PasswordChanged(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        => ViewModel.SignUpConfirmPassword = ((PasswordBox)sender).Password;
}