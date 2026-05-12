using DominiShop.Service;
using DominiShop.ViewModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

namespace DominiShop.View
{
    public sealed partial class MainPage : Page
    {
        private readonly SettingService _settingService;
        public MainViewModel ViewModel { get; } = App.Services.GetRequiredService<MainViewModel>();

        public MainPage()
        {
            InitializeComponent();

            _settingService = App.Services.GetRequiredService<SettingService>();

            var navService = (NavigationService)App.Services.GetRequiredService<INavigationService>();
            navService.Frame = this.ContentFrame;

            this.Loaded += MainPage_Loaded;
        }

        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            string lastPageTag = _settingService.GetLastVisitedPage();

            var itemToSelect = MainNavView.MenuItems.OfType<NavigationViewItem>()
                .FirstOrDefault(i => i.Tag?.ToString() == lastPageTag);

            if (itemToSelect == null)
            {
                itemToSelect = MainNavView.FooterMenuItems.OfType<NavigationViewItem>()
                    .FirstOrDefault(i => i.Tag?.ToString() == lastPageTag);
            }

            if (itemToSelect != null)
            {
                MainNavView.SelectedItem = itemToSelect;
                NavigateBasedOnTag(lastPageTag);
            }
            else
            {
                NavigateBasedOnTag("DashboardPage");
            }
        }

        private void MainNavView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            if (args.InvokedItemContainer != null)
            {
                string navTag = args.InvokedItemContainer.Tag.ToString();

                _settingService.SaveLastVisitedPage(navTag);

                NavigateBasedOnTag(navTag);
            }
        }

        private void NavigateBasedOnTag(string navTag)
        {
            var navService = (NavigationService)App.Services.GetRequiredService<INavigationService>();

            switch (navTag)
            {
                case "DashboardPage":
                    navService.NavigateTo(typeof(DashboardPage));
                    break;
                case "CustomerPage":
                    navService.NavigateTo(typeof(CustomerPage));
                    break;
                case "CategoryPage":
                    navService.NavigateTo(typeof(CategoryPage));
                    break;
                case "ProductPage":
                    navService.NavigateTo(typeof(ProductPage));
                    break;
                case "VoucherPage":
                    navService.NavigateTo(typeof(VoucherPage));
                    break;
                case "TaxPage":
                    navService.NavigateTo(typeof(TaxPage));
                    break;
                case "OrderPage":
                    navService.NavigateTo(typeof(OrderPage));
                    break;
                case "SettingsPage":
                    navService.NavigateTo(typeof(SettingsPage));
                    break;
            }
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            _settingService.SaveLastVisitedPage("DashboardPage");

            ViewModel.LogoutCommand.Execute(null);

            var navService = (NavigationService)App.Services.GetRequiredService<INavigationService>();
            navService.NavigateTo(typeof(AuthPage));
        }
    }
}