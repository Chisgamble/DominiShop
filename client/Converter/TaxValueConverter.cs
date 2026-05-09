using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.UI.Xaml.Data;
using DominiShop.Model;

namespace DominiShop.Converter;

public class TaxValueConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is not Tax tax) return "0";

        if (tax.Type == "Percentage")
        {
            return $"{tax.Value:N1}%";
        }else if (tax.Type == "Fixed Amount")
        {
            return string.Format("{0:N0} ₫", tax.Value);
        }

        return string.Format("0");
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}