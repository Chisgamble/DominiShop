using Microsoft.UI;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System;

namespace DominiShop.Converter;

// Chuyển status string → màu nền Badge
public class OrderStatusToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return (value as string)?.ToLower() switch
        {
            "pending"    => new SolidColorBrush(Colors.Orange),
            "confirmed"  => new SolidColorBrush(Colors.CornflowerBlue),
            "delivering" => new SolidColorBrush(Colors.MediumPurple),
            "completed"  => new SolidColorBrush(Colors.ForestGreen),
            "cancelled"  => new SolidColorBrush(Colors.Crimson),
            _            => new SolidColorBrush(Colors.Gray),
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}

// Chuyển status string → label tiếng Việt dễ đọc
public class OrderStatusToLabelConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return (value as string)?.ToLower() switch
        {
            "pending"    => "Chờ xử lý",
            "confirmed"  => "Đã xác nhận",
            "delivering" => "Đang giao",
            "completed"  => "Hoàn thành",
            "cancelled"  => "Đã huỷ",
            _            => value?.ToString() ?? "—",
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}
