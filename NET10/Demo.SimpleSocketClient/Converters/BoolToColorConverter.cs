using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace Demo.SimpleSocketClient.Converters;

/// <summary>
/// Bool 값을 색상으로 변환하는 컨버터
/// true: 녹색 (연결됨), false: 빨강 (연결 안 됨)
/// </summary>
public class BoolToColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isConnected)
        {
            return isConnected ? Colors.LimeGreen : Colors.Red;
        }
        return Colors.Gray;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is Color color)
        {
            // LimeGreen이면 true, Red면 false
            if (color == Colors.LimeGreen)
                return true;
            if (color == Colors.Red)
                return false;
        }

        // 기본값: 알 수 없는 색상이면 false
        return false;
    }
}