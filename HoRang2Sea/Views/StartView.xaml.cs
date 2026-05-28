using DevExpress.Xpf.LayoutControl;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace HoRang2Sea.Views
{
    /// <summary>
    /// StartView.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class StartView : UserControl
    {
        public StartView()
        {
            InitializeComponent();
            // 최초 표시 + 프로젝트 탭→Start 탭 복귀 시 모두 IsVisibleChanged 발생 → 영상 재시작 (탭 복귀 시 검정 화면 방지)
            this.IsVisibleChanged += (s, e) =>
            {
                if (e.NewValue is bool visible && visible)
                    StartMovie();
            };
        }

        // mp4 절대경로로 재생. 첫 프레임 준비 전까지 Hidden → 뒤 Border 배경색이 보임(검정 방지).
        // Source를 null→경로로 재설정해 같은 파일이어도 MediaOpened가 다시 발생(탭 복귀 시 재표시 보장).
        private void StartMovie()
        {
            try
            {
                if (myMediaElement == null) return;
                myMediaElement.Visibility = Visibility.Hidden;
                var moviePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "StartMovie.mp4");
                if (System.IO.File.Exists(moviePath))
                {
                    myMediaElement.Stop();
                    myMediaElement.Source = null;
                    myMediaElement.Source = new Uri(moviePath, UriKind.Absolute);
                    myMediaElement.Position = TimeSpan.Zero;
                    myMediaElement.Play();
                }
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"StartMovie load failed: {ex.Message}"); }
        }

        // 영상 첫 프레임이 준비되면 표시 (그 전엔 Hidden → Border 배경색만 보여 검정 화면 방지)
        private void MyMedia_MediaOpened(object sender, RoutedEventArgs e)
        {
            if (myMediaElement != null)
                myMediaElement.Visibility = Visibility.Visible;
        }

        // 반복 재생 (기존 RepeatBehavior=Forever 대체)
        private void MyMedia_MediaEnded(object sender, RoutedEventArgs e)
        {
            if (myMediaElement != null) { myMediaElement.Position = TimeSpan.Zero; myMediaElement.Play(); }
        }

        private void Tile_Click_1(object sender, EventArgs e)
        {
            string url = "https://hydrogen.or.kr/";
            Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
        }

        private void Tutorial_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.Tag is string vehicleName)
            {
                try
                {
                    var mainVm = App.Container.GetInstance<HoRang2Sea.ViewModels.MainViewModel>();
                    mainVm.CreatDocument(vehicleName);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Tutorial click error: {ex.Message}");
                }
            }
        }

        private void FuelCell_Click(object sender, MouseButtonEventArgs e)
        {
            string url = "https://hydrogen.or.kr/";
            Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
        }
        private void BarButtonItem_Click_Horang2(object sender, DevExpress.Xpf.Bars.ItemClickEventArgs e)
        {
            
            string url = "https://Horang2.kr";

            // 기본 브라우저에서 링크 열기
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        private void BarButtonItem_Click_Profile(object sender, DevExpress.Xpf.Bars.ItemClickEventArgs e)
        {

            string url = "http://211.253.10.68:9092";

            // 기본 브라우저에서 링크 열기
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
    }


    public class ScalablePaddingConverter : IValueConverter
    {
        public ScalablePaddingConverter()
        {
            MinPadding = 35;
        }

        object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var controlHeight = (double)value;
            double desiredContentHeight = 3 * Tile.LargeHeight + 2 * TileLayoutControl.DefaultItemSpace + 20;
            double paddingY = Math.Floor(Math.Max(0d, controlHeight - desiredContentHeight) / 2d);
            paddingY = Math.Max(MinPadding, Math.Min(paddingY, TileLayoutControl.DefaultPadding.Top));
            double relativePadding = (paddingY - MinPadding) / (TileLayoutControl.DefaultPadding.Top - MinPadding);
            double paddingX = Math.Floor(MinPadding + relativePadding * (TileLayoutControl.DefaultPadding.Left - MinPadding));
            return new Thickness(paddingX, paddingY, paddingX, paddingY);
        }
        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public double MinPadding { get; set; }
    }
}
