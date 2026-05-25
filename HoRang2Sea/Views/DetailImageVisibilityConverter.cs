using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace HoRang2Sea.Views
{
    /// <summary>
    /// PropertyImagePath(/Resource/Detail/{Component}Detail.svg)가 실제 존재하는 컴포넌트
    /// 상세 SVG일 때만 Visible, 없으면 Collapsed. 누락 컴포넌트 클릭 시 이미지 공란 영역 제거.
    /// </summary>
    public class DetailImageVisibilityConverter : IValueConverter
    {
        private static readonly HashSet<string> Available = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Air Humidifier", "Battery", "Ejector", "Intercooler", "Radiator", "Reducer", "Valve"
        };

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var path = value as string;
            if (string.IsNullOrWhiteSpace(path)) return Visibility.Collapsed;
            try
            {
                var file = System.IO.Path.GetFileNameWithoutExtension(path);
                const string suffix = "Detail";
                var name = file.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)
                    ? file.Substring(0, file.Length - suffix.Length)
                    : file;
                return Available.Contains(name) ? Visibility.Visible : Visibility.Collapsed;
            }
            catch
            {
                return Visibility.Collapsed;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
