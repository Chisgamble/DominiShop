using System;
using System.Collections.Generic;
using System.Text;

namespace DominiShop.Service
{
    public interface INavigationService
    {
        void NavigateTo(Type pageType, object? parameter = null);
        void GoBack();
    }
}
