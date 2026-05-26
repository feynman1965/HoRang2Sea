using System;
using System.Globalization;
using System.Reflection;
using System.Windows;
using System.Windows.Data;

namespace HoRang2Sea.Views
{
    /// <summary>
    /// Property 패널 컴포넌트 상세 이미지 경로 해석: Detail SVG(상세 일러스트, 일부만 존재)가
    /// 실존하면 pack URI 반환, 없으면 null. ⚠️ ModelImage는 노드(버튼) 아이콘이라 용도 다름 → 폴백 금지.
    /// (Property는 dx:DXImage로 SVG 렌더 — 네이티브 Image는 SVG 렌더 불가)
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
            return TryRel($"Resource/Detail/{name}Detail.svg");
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
