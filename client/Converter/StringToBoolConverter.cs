using Microsoft.UI.Xaml.Data;
using System;

namespace DominiShop.Converter
{
    public class StringToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            // Returns true if the string has content, false if it's null or empty
            if (value is string str)
            {
                return !string.IsNullOrWhiteSpace(str);
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            // ConvertBack is rarely used for this type of converter
            throw new NotImplementedException();
        }
    }
}

