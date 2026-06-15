using System;
using System.Collections.Generic;
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
    /// PostTimeChartView.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class PostTimeChartView : UserControl
    {
        public PostTimeChartView()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            //UserToolbar.TargetSurface = "{Binding Source={x:Reference Name=sciChart1}}"
        }
        private void PopOutChart_Click(object sender, RoutedEventArgs e)
        {
            var chartView = new PostTimeChartView { DataContext = this.DataContext };
            var window = new Window
            {
                Title = "Monitor Chart - Large View (Zoom: wheel/drag, Reset: double-click)",
                Width = 1400,
                Height = 800,
                Content = chartView,
                Owner = System.Windows.Application.Current?.MainWindow,
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };
            // 팝업이 닫힐 때 popup chartView의 DialogService 등록이 공유 VM에 남아
            // "닫힌 창에는 owner 속성을 설정할 수 없습니다" 오류를 일으킴.
            // Content를 비워 Unloaded를 유발해 behavior를 분리한다.
            window.Closed += (_, __) =>
            {
                window.Content = null;
                chartView.DataContext = null;
            };
            window.Show();
        }

        // 변수 탭 스트립: 마우스 휠로 좌우 스크롤
        private void ChartTabList_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var sv = FindVisualChild<ScrollViewer>(sender as DependencyObject);
            if (sv != null)
            {
                sv.ScrollToHorizontalOffset(sv.HorizontalOffset - e.Delta);
                e.Handled = true;
            }
        }

        private static T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null) return null;
            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T t) return t;
                var result = FindVisualChild<T>(child);
                if (result != null) return result;
            }
            return null;
        }
    }
}