using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DominiShop.Service;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;

namespace DominiShop.ViewModel
{
    public partial class MainViewModel : BaseViewModel
    {
        private readonly INavigationService _navigationService;
        private readonly AuthService _authService;
        private readonly ConfigService _configService; 

        [ObservableProperty]
        public partial string CurrentUserName { get; set; } = "Loading...";

        [ObservableProperty]
        public partial string CurrentUserEmail { get; set; } = "email@example.com";

        [ObservableProperty]
        public partial Microsoft.UI.Xaml.Media.ImageSource? ProfileImage { get; set; }

        public MainViewModel(INavigationService navigationService, AuthService authService, ConfigService configService)
        {
            _navigationService = navigationService;
            _authService = authService;
            _configService = configService;

            LoadUserData();
        }

        private void LoadUserData()
        {
            var user = _authService.CurrentUser;
            if (user != null)
            {
                if (user.UserMetadata != null && user.UserMetadata.TryGetValue("username", out var name))
                {
                    CurrentUserName = name.ToString();
                }
                else
                {
                    CurrentUserName = user.Email.Split('@')[0];
                }

                CurrentUserEmail = user.Email;
            }
        }

        [RelayCommand]
        private async Task LogoutAsync()
        {
            _configService.ClearCredentials();

            await _authService.LogoutAsync();

            CurrentUserName = "Guest";
            CurrentUserEmail = string.Empty;
            ProfileImage = null;
        }

        [RelayCommand]
        private void OnItemInvoked(NavigationViewItemInvokedEventArgs args)
        {
            if (args.IsSettingsInvoked)
            {
                // _navigationService.NavigateTo(typeof(SettingsPage));
            }
            else if (args.InvokedItemContainer is NavigationViewItem item)
            {
                var pageType = Type.GetType($"DominiShop.View.{item.Tag}");
                if (pageType != null)
                {
                    _navigationService.NavigateTo(pageType);
                }
            }
        }

        public void LoadUserData(string name, string email)
        {
            CurrentUserName = name;
            CurrentUserEmail = email;
        }
    }
}