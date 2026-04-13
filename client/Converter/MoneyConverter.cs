using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Data;

namespace DominiShop.Converter;

public class MoneyConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is long amount)
            return string.Format("{0:N0} ₫", amount);
        if (value is decimal d)
            return string.Format("{0:N0} ₫", d);
        return "0 ₫";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}