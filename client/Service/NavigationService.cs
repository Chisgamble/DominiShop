using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Text;

namespace DominiShop.Service
{
    public class NavigationService : INavigationService
    {
        public Frame? Frame { get; set; }

        public void NavigateTo(Type pageType, object? parameter = null)
        {
            if (Frame?.CurrentSourcePageType != pageType)
            {
                Frame?.Navigate(pageType, parameter);
            }
        }

        public void GoBack() => Frame?.GoBack();
    }
}
