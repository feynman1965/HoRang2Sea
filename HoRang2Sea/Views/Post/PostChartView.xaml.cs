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
    /// PostChartView.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class PostChartView : UserControl
    {
        public PostChartView()
        {
            InitializeComponent();
        }

        // ===== 차트 인터랙션 (0624 피드백: 줌/팬/수동 스케일) =====
        // 사용자 조작(클릭/휠) 시 자동범위 해제 → 줌/팬이 스냅백되지 않음
        private void Chart_UserInteractDown(object sender, MouseButtonEventArgs e)
            => (DataContext as ViewModels.PostChartViewModel)?.SetAxesAutoRange(false);

        private void Chart_UserInteractWheel(object sender, MouseWheelEventArgs e)
            => (DataContext as ViewModels.PostChartViewModel)?.SetAxesAutoRange(false);

        // Fit: 전체 데이터 범위로 (수동 모드 유지 — Auto 버튼으로 라이브 추적 복귀)
        private void FitChart_Click(object sender, RoutedEventArgs e)
        {
            (DataContext as ViewModels.PostChartViewModel)?.SetAxesAutoRange(false);
            sciChart.ZoomExtents();
        }

        // Auto: 자동범위 복귀 (시뮬 진행을 따라감)
        private void AutoRange_Click(object sender, RoutedEventArgs e)
            => (DataContext as ViewModels.PostChartViewModel)?.SetAxesAutoRange(true);

        // 축 범위 직접 입력 (키보드)
        private void AxisRange_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is ViewModels.PostChartViewModel vm)
                ChartAxisRangeDialog.Show(vm.XAxes, vm.YAxes);
        }
    }
}
