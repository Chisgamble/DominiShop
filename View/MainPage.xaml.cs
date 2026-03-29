using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace DominiShop.View
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainViewModel ViewModel { get; set; } = new();
        public MainPage()
        {
            InitializeComponent();
        }

        public string FormatTelephone(string telephone)
        {
            if (string.IsNullOrEmpty(telephone) || telephone.Length != 10)
                return telephone; // Trả về số điện thoại gốc nếu không hợp lệ
            return $"{telephone.Substring(0, 3)}-{telephone.Substring(3, 3)}-{telephone.Substring(6, 4)}";
        }

        public string FormatCredit(int value, int max)
        {
            CultureInfo culture = CultureInfo.GetCultureInfo("en-US");
            var percent = value * 1.0 / max * 100;
            var formatted = $"{value}/{max} {string.Format(culture, "{0:F2}", percent)}%";
            return formatted;
        }

        private void updateButton_Click(object sender, RoutedEventArgs e)
        {
            //ViewModel.Active?.ID = "007";
            //ViewModel.Active?.FullName = "James Bond"; 

            ViewModel.Active = new Student
            {
                ID = "002",
                FullName = "Nguyễn Văn A"
            };

            //string info = TenGiCungDuoc.FormattedTelephone(ViewModel.Active.Telephone);
            string info = ViewModel.Active.Telephone.FormattedTelephone();
        }
    }
}
