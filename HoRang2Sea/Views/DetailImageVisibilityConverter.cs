using System;
using System.Globalization;
using System.Reflection;
using System.Windows;
using System.Windows.Data;

namespace HoRang2Sea.Views
{
    /// <summary>
    /// Property 패널 컴포넌트 이미지 경로 해석: Detail SVG(일부) → ModelImage SVG(대부분) 순으로
    /// 실존 리소스의 pack URI 반환, 둘 다 없으면 null. (Property는 dx:DXImage로 SVG 렌더)
    /// </summary>
    internal static class PropertyImageResolver
    {
        public static string Resolve(string propertyImagePath)
        {
            if (string.IsNullOrWhiteSpace(propertyImagePath)) return null;
            string name;
            try
            {
                var file = System.IO.Path.GetFileNameWithoutExtension(propertyImagePath);
                const string suffix = "Detail";
                name = file.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)
                    ? file.Substring(0, file.Length - suffix.Length)
                    : file;
            }
            catch { return null; }

            var asm = Assembly.GetExecutingAssembly().GetName().Name;
            string TryRel(string rel)
            {
                try
                {
                    var info = Application.GetResourceStream(new Uri(rel, UriKind.Relative));
                    if (info != null) return $"pack://application:,,,/{asm};component/{rel}";
                }
                catch { }
                return null;
            }
            return TryRel($"Resource/Detail/{name}Detail.svg") ?? TryRel($"Resource/ModelImage/{name}.svg");
        }
    }

    public class PropertyImageSourceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => PropertyImageResolver.Resolve(value as string);

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class DetailImageVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => string.IsNullOrEmpty(PropertyImageResolver.Resolve(value as string))
                ? Visibility.Collapsed
                : Visibility.Visible;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
