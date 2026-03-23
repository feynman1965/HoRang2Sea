using DevExpress.Data.Filtering;
using DevExpress.Data.Filtering.Helpers;
using DevExpress.Diagram.Core;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Diagram;
using DevExpress.Xpf.Diagram.Themes;
using HoRang2Sea.Models;
using HoRang2Sea.ViewModels;
using System.Windows.Media;
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
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ColumnDefinition = HoRang2Sea.Models.ColumnDefinition;


using DevExpress.Xpf.Docking;
using SciChart.Data.Model;
using SciChart.Charting.ChartModifiers;
using SciChart.Charting.Visuals.Axes;
using SciChart.Charting.Visuals.RenderableSeries;
using SciChart.Charting.Visuals;
using SciChart.Charting3D.Primitives;
using SciChart.Charting.Model.DataSeries;
using SciChart.Charting.Visuals.Annotations;
using HoRang2Sea.Models;


namespace HoRang2Sea.Views
{
    /// <summary>
    /// FishingBoatModuleView.xaml에 대한 상호 작용 논리
    /// </summary>
    /// 
    public partial class FishingBoatModuleView : UserControl
    {
        TableRelationEvaluationOperator evaluationOperator;
        public FishingBoatModuleView()
        {
            evaluationOperator = new TableRelationEvaluationOperator();
            CriteriaOperator.RegisterCustomFunction(evaluationOperator);
            InitializeComponent();


        }
        private void VelocityProfilePanel_Loaded(object sender, RoutedEventArgs e)
        {
            var panel = sender as DocumentPanel;
            if (panel != null)
            {
                // SciChartSurface 생성 및 설정
                var sciChartSurface = new SciChartSurface();
                SciChartSurface.SetRuntimeLicenseKey("AcrmWhcNT5lkRmD8o7x6Jw6hJZmQeRZZotGeSqetKfJ4rgmsMa+hTlfyNvSdSUb4tTdY0Ciq0QzfQkGNh5T4RqdJLddbI8m2fTL+CAJheiTf4Ne0s0YrGwQVocGUtRU1h2pXA4IUygx3fH4ZLPuwHrsHPtImEmt7KIg0dgeE+2lUi7OxCZLkAO/oRWM0m2Vxkd76SLWft/t7EW0diJzq7E0EZLW7Cqfufsy1/okz+mbKHy0DkD5AALbL2xkj0gxfxjRd1BLTCzTkvDvApsLZiH1ZJXkQpJI45S5M5sDnnjK6XMHfewLXUXeqe5bmmbF+VO+ZPmfuNNqJY9NibjPJTzDHl1kBxXeA69Pip7bm3K+X+4EutixKWCM+feZy+xIkjP2S0cyWIa32nEn21OTTN+hbNiUeQmT3sHxK6gtZGOk9oSsvNYZtT+jp3hWtn4/WtHp29FibfY3t6Lf89n8JQaJIw6gjm6Dzf17rXMY3lEOfFXIQACI50lqkgeQhV+M22bwh8j5MoZ6e9l/qQqTJLh8/0xyuIJSt8TdOtUCeA3sQyfaQ/ez3qkPTOGVvHmgUsePvSX15QqdBbJX5RBj1nMhcmiiyBwzP7PffbQuqXlIv9oXHQF4t/6kfkEUkT5PY8IJiYF812d3YgCtTgBw0UMgBeoKPJRyybPQNB7X/SUWuRkZ+8Q==");
                sciChartSurface.Background = Brushes.White;
                // 축 설정
                var xAxis = new NumericAxis
                {
                    AxisTitle = "Time (seconds)", // AxisTitle은 단순 문자열 설정
                    FontSize = 14.0, // 폰트 크기 직접 설정
                    Foreground = Brushes.Black, // 전경색 설정
                    TextFormatting = "0.0",
                    DrawMajorGridLines = true,
                    DrawMinorGridLines = false
                };

                var yAxis = new NumericAxis
                {
                    AxisTitle = "Velocity (km/h)",
                    FontSize = 14.0, // 폰트 크기 직접 설정
                    Foreground = Brushes.Black, // 전경색 설정
                    TextFormatting = "0.0",
                    DrawMajorGridLines = true,
                    DrawMinorGridLines = false,
                    VisibleRange = new DoubleRange(0, 150)
                };

                sciChartSurface.XAxis = xAxis;
                sciChartSurface.YAxis = yAxis;



                // 그래프 제목 추가
                var titleAnnotation = new TextAnnotation
                {
                    Text = "Velocity Profile", // 그래프 제목
                    FontSize = 20,
                    Foreground = Brushes.Black,
                    HorizontalAnchorPoint = HorizontalAnchorPoint.Center,
                    VerticalAnchorPoint = VerticalAnchorPoint.Top,
                    X1 = 0.5, // 화면의 중앙으로 설정
                    Y1 = 0, // 그래프 상단에 위치시킴
                    CoordinateMode = AnnotationCoordinateMode.Relative
                };

                sciChartSurface.Annotations.Add(titleAnnotation);


                // Renderable Series 생성
                var lineSeries = new FastLineRenderableSeries
                {
                    StrokeThickness = 2,
                    Stroke = Colors.Blue
                };

                // ViewModel에서 데이터 시리즈 가져오기
                var viewModel = this.DataContext as FishingBoatModuleViewModel;
                if (viewModel != null)
                {
                    lineSeries.DataSeries = viewModel.VelocityLineDataSeries;

                    // 데이터에 따라 x축 범위 설정
                    if (viewModel.VelocityLineDataSeries.Count > 0)
                    {
                        double minX = viewModel.VelocityLineDataSeries.XValues.First() - 10;
                        double maxX = viewModel.VelocityLineDataSeries.XValues.Last() + 10;
                        xAxis.VisibleRange = new DoubleRange(minX, maxX);
                    }
                }

                // Renderable Series 추가
                sciChartSurface.RenderableSeries.Add(lineSeries);

                // 차트 모디파이어 추가 (선택 사항)
                var modifiers = new ModifierGroup();
                modifiers.ChildModifiers.Add(new ZoomPanModifier());
                modifiers.ChildModifiers.Add(new RolloverModifier());
                sciChartSurface.ChartModifier = modifiers;

                // panel의 Content로 SciChartSurface 설정
                panel.Content = sciChartSurface;
            }
        }

    }
}
