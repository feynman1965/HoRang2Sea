using DevExpress.CodeParser;
using DevExpress.Xpf.Core;
using System;
using System.Threading.Tasks;
using System.Windows.Media;

namespace HoRang2Sea.Common
{
    public class CommonFunction
    {
        static public ImageSource GetGlyphFromUri(string uri)
        {
            var svgImageSource = new SvgImageSourceExtension() { Uri = new Uri(uri) }.ProvideValue(null);
            return (ImageSource)svgImageSource;
        }
    }
}
