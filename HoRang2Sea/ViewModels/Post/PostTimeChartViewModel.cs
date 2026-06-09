using DevExpress.Mvvm;
using HoRang2Sea.Models;
using SciChart.Charting.Model.ChartSeries;
using SciChart.Charting.Model.DataSeries;
using SciChart.Charting.Visuals.Axes;
using SciChart.Data.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Windows.Media;
using Thickness = System.Windows.Thickness;

namespace HoRang2Sea.ViewModels
{
    public class PostTimeChartViewModel : PostPanelWorkspaceViewModel
    {
        private bool _isPropertyDialogOpen;
        private double savedTimer = 0; // 일시정지 시 timer 저장
        private bool isPausedState = false; // 일시정지 상태 추적

        public PostTimeChartViewModel(string displayName, DocumentViewModel parent = null)
        {
            DisplayName = displayName;
            IsClosed = false;
            IsActive = true;
            ChartXItems = new ObservableCollection<string>();
            ChartYItems = new ObservableCollection<string>();
            ChartGlobalItems = new ObservableCollection<string>();
            XAxes = new ObservableCollection<IAxisViewModel>();
            YAxes = new ObservableCollection<IAxisViewModel>();

            RenderableSeries = new ObservableCollection<IRenderableSeriesViewModel>();
            OnChartPropertyCommand = new DelegateCommand(OnChartProperty);
            ParentViewModel = parent;

            if (_isPropertyDialogOpen) return;
            ChartYItems.CollectionChanged += ChartYItems_CollectionChanged;
        }

        private void ChartYItems_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (_isPropertyDialogOpen) return;
            UpdateChartGlobalItems();
            UpdateFilteredList();
        }

        private void UpdateChartGlobalItems()
        {
            if (_isPropertyDialogOpen) return;
            List<string> newItems = null;

            if (BaseMWModel is FishingBoatMW FishingBoatMW)
            {
                newItems = FishingBoatMW.FishingBoatMWOuts
                    .Where(mw => !ChartYItems.Contains(mw.Name))
                    .Select(wd => wd.Name)
                    .ToList();
            }
            else if (BaseMWModel is PortGuideShipMW PortGuideShipMW)
            {
                newItems = PortGuideShipMW.PortGuideShipMWOuts
                    .Where(mw => !ChartYItems.Contains(mw.Name))
                    .Select(wd => wd.Name)
                    .ToList();
            }
            else if (BaseMWModel is TrainingShipMW TrainingShipMW)
            {
                newItems = TrainingShipMW.TrainingShipMWOuts
                    .Where(mw => !ChartYItems.Contains(mw.Name))
                    .Select(wd => wd.Name)
                    .ToList();
            }

            if (newItems != null)
            {
                ChartGlobalItems.Clear();
                foreach (var item in newItems)
                {
                    ChartGlobalItems.Add(item);
                }
            }
        }

        public ObservableCollection<string> ChartXItems { get; set; }
        public ObservableCollection<string> ChartYItems { get; set; }
        public ObservableCollection<string> ChartGlobalItems { get; set; }

        public ObservableCollection<IAxisViewModel> YAxes { get; set; }
        public ObservableCollection<IAxisViewModel> XAxes { get; set; }
        public ObservableCollection<IRenderableSeriesViewModel> RenderableSeries { get; set; }
        public ObservableCollection<XyDataSeries<double, double>> lineData = new ObservableCollection<XyDataSeries<double, double>>();

        // ── 변수별 보기 탭 ──────────────────────────────────────────────
        // 각 변수의 series/axis 참조. 탭 전환 시 가시성만 토글한다(데이터 append 경로는 그대로 → 저비용).
        private readonly List<(string name, LineRenderableSeriesViewModel series, NumericAxisViewModel axis)> _seriesRefs
            = new List<(string, LineRenderableSeriesViewModel, NumericAxisViewModel)>();

        public ObservableCollection<string> ChartTabs { get; set; } = new ObservableCollection<string>();

        private string _selectedChartTab = "전체";
        public string SelectedChartTab
        {
            get => _selectedChartTab;
            set { if (SetValue(ref _selectedChartTab, value)) ApplyTabView(); }
        }

        private bool _hasChartTabs;
        public bool HasChartTabs
        {
            get => _hasChartTabs;
            set => SetValue(ref _hasChartTabs, value);
        }

        // 차트 하단 범례: "전체" 탭(오버레이)에서만 표시, 개별 변수 탭에선 숨김
        private bool _showChartLegend = true;
        public bool ShowChartLegend
        {
            get => _showChartLegend;
            set => SetValue(ref _showChartLegend, value);
        }

        // ChartYItems 기준으로 탭 목록 재구성: "전체"(오버레이) + 각 변수
        private void RebuildChartTabs()
        {
            ChartTabs.Clear();
            ChartTabs.Add("전체");
            foreach (var name in ChartYItems.Distinct())
                ChartTabs.Add(name);
            HasChartTabs = ChartYItems.Count > 0;
            _selectedChartTab = "전체";
            RaisePropertyChanged(nameof(SelectedChartTab));
            ApplyTabView();
        }

        // 선택 탭에 해당하는 series/axis 만 표시. 데이터는 모든 series 에 계속 append 되므로 탭 전환만으로 즉시 전환된다.
        private void ApplyTabView()
        {
            string tab = _selectedChartTab;
            bool all = string.IsNullOrEmpty(tab) || tab == "전체";
            ShowChartLegend = all;   // 전체 탭에서만 범례 표시
            foreach (var r in _seriesRefs)
            {
                bool show = all || r.name == tab;
                r.series.IsVisible = show;
                r.axis.Visibility = show ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
            }
        }

        public DelegateCommand OnChartPropertyCommand { get; set; }

        protected override string WorkspaceName { get { return "BottomHost"; } }

        public DocumentViewModel ParentViewModel { get; set; }
        public double timer { get; set; }

        private BaseModel BaseMWModel
        {
            get
            {
                if (ParentViewModel is FishingBoatModuleViewModel fishingBoatVm)
                    return fishingBoatVm.BaseMWModel;
                else if (ParentViewModel is PortGuideShipModuleViewModel portGuideShipVm)
                    return portGuideShipVm.BaseMWModel;
                else if (ParentViewModel is TrainingShipModuleViewModel trainingShipVm)
                    return trainingShipVm.BaseMWModel;
                else if (ParentViewModel is PostViewModel postVm)
                    return postVm.BaseMWModel;
                return null;
            }
        }


        private string _searchKeyword;
        private ObservableCollection<string> _filteredChartGlobalItems;

        public string SearchKeyword
        {
            get => _searchKeyword;
            set
            {
                SetValue(ref _searchKeyword, value);
                UpdateFilteredList();
            }
        }

        public ObservableCollection<string> FilteredChartGlobalItems
        {
            get => _filteredChartGlobalItems;
            set => SetValue(ref _filteredChartGlobalItems, value);
        }

        // 컴포넌트 트리(좌측 Global Items)용. FilteredChartGlobalItems 를 접두어로 그룹화한 표시용 컬렉션.
        private ObservableCollection<ChartVariableGroup> _groupedChartGlobalItems;
        public ObservableCollection<ChartVariableGroup> GroupedChartGlobalItems
        {
            get => _groupedChartGlobalItems;
            set => SetValue(ref _groupedChartGlobalItems, value);
        }

        private void UpdateFilteredList()
        {
            if (FilteredChartGlobalItems == null)
            {
                FilteredChartGlobalItems = new ObservableCollection<string>();
            }

            FilteredChartGlobalItems.Clear();

            if (string.IsNullOrEmpty(SearchKeyword))
            {
                foreach (var item in ChartGlobalItems)
                {
                    FilteredChartGlobalItems.Add(item);
                }
            }
            else
            {
                var filtered = ChartGlobalItems
                    .Where(item => item != null && item.IndexOf(SearchKeyword, StringComparison.OrdinalIgnoreCase) >= 0);

                foreach (var item in filtered)
                {
                    FilteredChartGlobalItems.Add(item);
                }
            }

            RaisePropertyChanged(nameof(FilteredChartGlobalItems));

            // 트리(그룹) 갱신: 검색 중이면 펼친 상태로
            GroupedChartGlobalItems = ChartVariableGroup.Build(FilteredChartGlobalItems, expandAll: true);
        }

        // 컴포넌트 트리에서 변수를 더블클릭 → Y축 항목으로 추가 (기존 드래그-드롭과 동일 동작·4개 제한 유지)
        public void AddYItem(string name)
        {
            if (string.IsNullOrEmpty(name)) return;
            if (ChartYItems.Contains(name)) return;

            ChartYItems.Add(name);
            if (ChartGlobalItems != null) ChartGlobalItems.Remove(name);
            UpdateFilteredList();
        }

        // 우측 Y 목록에서 더블클릭 → 제거 (다시 좌측 트리로 복귀)
        public void RemoveYItem(string name)
        {
            if (string.IsNullOrEmpty(name)) return;
            if (!ChartYItems.Contains(name)) return;

            ChartYItems.Remove(name);
            if (ChartGlobalItems != null && !ChartGlobalItems.Contains(name))
                ChartGlobalItems.Add(name);
            UpdateFilteredList();
        }


        public void OnChartProperty()
        {
            _isPropertyDialogOpen = true;
            try
            {
                List<string> newItems = null;

                if (BaseMWModel is FishingBoatMW FishingBoatMW)
                {
                    newItems = FishingBoatMW.FishingBoatMWOuts
                        .Where(mw => !ChartYItems.Any(i => i == mw.Name))
                        .Select(wd => wd.Name)
                        .ToList();
                }
                else if (BaseMWModel is PortGuideShipMW PortGuideShipMW)
                {
                    newItems = PortGuideShipMW.PortGuideShipMWOuts
                        .Where(mw => !ChartYItems.Any(i => i == mw.Name))
                        .Select(wd => wd.Name)
                        .ToList();
                }
                else if (BaseMWModel is TrainingShipMW TrainingShipMW)
                {
                    newItems = TrainingShipMW.TrainingShipMWOuts
                        .Where(mw => !ChartYItems.Any(i => i == mw.Name))
                        .Select(wd => wd.Name)
                        .ToList();
                }

                if (newItems != null)
                {
                    ChartGlobalItems.Clear();
                    foreach (var item in newItems)
                    {
                        ChartGlobalItems.Add(item);
                    }
                }

                UpdateFilteredList();

                if (FilteredChartGlobalItems == null || FilteredChartGlobalItems.Count == 0)
                {
                    return;
                }

                UICommand rOkCommand = new UICommand()
                {
                    Caption = "Ok",
                    IsDefault = true,
                    Command = new DelegateCommand(() =>
                    {
                        try
                        {
                            ChartSet(true);   // 사용자가 변수 추가/변경 → 기록 버퍼에서 backfill (시뮬 후에도 표시)
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"ChartSet Error: {ex.Message}");
                        }
                    })
                };

                UICommand rCloseCommand = new UICommand()
                {
                    Caption = "Close",
                    IsCancel = true
                };

                IDialogService service = this.GetService<IDialogService>("ChartPropertyDialogService");
                UICommand result = service.ShowDialog(
                    dialogCommands: new[] { rOkCommand, rCloseCommand },
                    title: "Import Data",
                    viewModel: this
                );
            }
            finally
            {
                _isPropertyDialogOpen = false;
                UpdateChartGlobalItems();
                UpdateFilteredList();
            }
        }

        // backfill=true 면 기록 버퍼에서 각 변수 series 를 되채움(시뮬 후/중 임의 변수 표시).
        // 실행 시작 시 호출되는 ChartSet 은 backfill=false 라 기존 동작 유지.
        public void ChartSet(bool backfill = false)
        {
            if (System.Windows.Application.Current?.Dispatcher.CheckAccess() == false)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() => ChartSet(backfill));
                return;
            }

            foreach (var data in lineData)
            {
                data?.Clear();
            }

            RenderableSeries.Clear();
            lineData.Clear();
            XAxes.Clear();
            YAxes.Clear();
            _seriesRefs.Clear();

            // 일시정지 상태에서 재개되는 경우 timer 복원, 아니면 0으로 초기화
            if (isPausedState)
            {
                timer = savedTimer;
                isPausedState = false;
                Debug.WriteLine($"TimeChart 일시정지에서 재개: timer 복원 = {timer:F3}");
            }
            else
            {
                timer = 0;
                Debug.WriteLine($"TimeChart 새로 시작: timer = 0");
            }

            string XAxis = "Time";

            var xNumAxis = new NumericAxisViewModel
            {
                AutoRange = AutoRange.Always,
                Id = XAxis,
                AxisAlignment = AxisAlignment.Bottom,
                AxisTitle = "Time [sec]",
                DrawMajorBands = false,
                TextFormatting = "0.00",
                VisibleRange = new DoubleRange(0, 1000),
                BorderBrush = new SolidColorBrush(Colors.CadetBlue)
            };
            XAxes.Add(xNumAxis);

            Thickness ythick = new Thickness(0, 0, 1, 0);

            Color[] fixedPalette = new Color[]
            {
                Color.FromRgb(31, 119, 180),   // Blue
                Color.FromRgb(214, 39, 40),    // Red
                Color.FromRgb(44, 160, 44),    // Green
                Color.FromRgb(255, 127, 14),   // Orange
                Color.FromRgb(148, 103, 189),  // Purple
                Color.FromRgb(140, 86, 75),    // Brown
                Color.FromRgb(227, 119, 194),  // Pink
                Color.FromRgb(127, 127, 127),  // Gray
                Color.FromRgb(188, 189, 34),   // Olive
                Color.FromRgb(23, 190, 207),   // Cyan
                Color.FromRgb(255, 152, 150),  // Salmon
                Color.FromRgb(23, 100, 60)     // Dark Green
            };
            int colorIdx = 0;
            foreach (var Chartitem in ChartYItems.Distinct())
            {
                string YAxis = Chartitem + "Y";
                var yNumAxis = new NumericAxisViewModel
                {
                    AutoRange = AutoRange.Always,
                    GrowBy = new DoubleRange(1.0, 1.0),   // Y축 여유(데이터 ~33% 차지). 라이브·시뮬후 공통. 값↑=더 여유
                    AxisAlignment = AxisAlignment.Left,
                    AxisTitle = Chartitem,
                    DrawMajorBands = true,
                    BorderThickness = ythick,
                    TextFormatting = "0.000",
                    Id = YAxis,
                };
                YAxes.Add(yNumAxis);

                XyDataSeries<double, double> newLineData = new XyDataSeries<double, double>();
                newLineData.SeriesName = Chartitem;
                newLineData.AcceptsUnsortedData = true;
                // FifoCapacity 제거: 데이터가 0부터 쌓이도록 함 (롤링 윈도우 비활성화)
                lineData.Add(newLineData);

                // 시뮬 후/중 임의 변수 표시: 기록 버퍼에서 series 되채움 (실행 시작 시엔 버퍼 비어 no-op)
                if (backfill && BaseMWModel is GenericPortDllModel recModel)
                {
                    var rec = recModel.GetRecordedSeries(Chartitem);
                    double ymin = double.MaxValue, ymax = double.MinValue; int cnt = 0;
                    foreach (var pt in rec)
                    {
                        newLineData.Append(pt.x, pt.y);
                        if (!double.IsNaN(pt.y) && !double.IsInfinity(pt.y))
                        {
                            if (pt.y < ymin) ymin = pt.y;
                            if (pt.y > ymax) ymax = pt.y;
                            cnt++;
                        }
                    }
                    // 상수/전NaN만 명시범위(AutoRange가 0폭이라 축이 안 보임). 그 외는 라이브와 동일하게 AutoRange+GrowBy 사용(동일 여유).
                    if (cnt == 0)
                    {
                        yNumAxis.AutoRange = AutoRange.Never;
                        yNumAxis.VisibleRange = new DoubleRange(-1, 1);
                    }
                    else if (ymin == ymax)
                    {
                        double pad = System.Math.Max(System.Math.Abs(ymin) * 0.5, 0.5);
                        yNumAxis.AutoRange = AutoRange.Never;
                        yNumAxis.VisibleRange = new DoubleRange(ymin - pad, ymax + pad);
                    }
                }

                Color seriesColor = fixedPalette[colorIdx % fixedPalette.Length];

                var newRenderableSeries = new LineRenderableSeriesViewModel
                {
                    StrokeThickness = 3,
                    Stroke = seriesColor,
                    DataSeries = newLineData,
                    YAxisId = YAxis,
                    XAxisId = XAxis,
                };

                RenderableSeries.Add(newRenderableSeries);
                _seriesRefs.Add((Chartitem, newRenderableSeries, yNumAxis));
                colorIdx++;
            }

            RebuildChartTabs();
        }

        public void ChartUpdate()
        {
            // RenderableSeries가 없는데 ChartYItems가 있으면 자동으로 ChartSet() 호출
            if (RenderableSeries.Count == 0 && ChartYItems.Count > 0)
            {
                Debug.WriteLine($"TimeChart: ChartSet() 자동 호출 (ChartYItems={ChartYItems.Count})");
                ChartSet();
                return;
            }
            else if (RenderableSeries.Count == 0 && ChartYItems.Count == 0)
            {
                // 아무 차트도 설정되지 않음 - 정상
                return;
            }

            try
            {
                if (ParentViewModel == null || BaseMWModel == null) return;

                if (BaseMWModel is FishingBoatMW FishingBoatMW)
                {
                    foreach (var Chartitem in ChartYItems.Distinct())
                    {
                        var data = FishingBoatMW.FishingBoatMWOuts.FirstOrDefault(d => d.Name == Chartitem);
                        if (data != null)
                        {
                            double xvalue = timer;

                            foreach (var renderableSeries in RenderableSeries)
                            {
                                var Chart = (LineRenderableSeriesViewModel)renderableSeries;

                                var ChartData = (XyDataSeries<double, double>)Chart.DataSeries;
                                var ChartName = ChartData.SeriesName;

                                if (ChartName.Equals(data.Name))
                                {
                                    ChartData.Append(xvalue, data.Value);
                                }
                            }
                        }
                    }

                    timer += 0.1; // interval 에 따른 조정 필요
                }
                else if (BaseMWModel is PortGuideShipMW PortGuideShipMW)
                {
                    foreach (var Chartitem in ChartYItems.Distinct())
                    {
                        var data = PortGuideShipMW.PortGuideShipMWOuts.FirstOrDefault(d => d.Name == Chartitem);
                        if (data != null)
                        {
                            double xvalue = timer;

                            foreach (var renderableSeries in RenderableSeries)
                            {
                                var Chart = (LineRenderableSeriesViewModel)renderableSeries;

                                var ChartData = (XyDataSeries<double, double>)Chart.DataSeries;
                                var ChartName = ChartData.SeriesName;

                                if (ChartName.Equals(data.Name))
                                {
                                    ChartData.Append(xvalue, data.Value);
                                }
                            }
                        }
                    }
                    timer += 0.1; // interval 에 따른 조정 필요
                }


                else if (BaseMWModel is TrainingShipMW TrainingShipMW)
                {
                    foreach (var Chartitem in ChartYItems.Distinct())
                    {
                        var data = TrainingShipMW.TrainingShipMWOuts.FirstOrDefault(d => d.Name == Chartitem);
                        if (data != null)
                        {
                            double xvalue = timer;

                            foreach (var renderableSeries in RenderableSeries)
                            {
                                var Chart = (LineRenderableSeriesViewModel)renderableSeries;

                                var ChartData = (XyDataSeries<double, double>)Chart.DataSeries;
                                var ChartName = ChartData.SeriesName;

                                if (ChartName.Equals(data.Name))
                                {
                                    ChartData.Append(xvalue, data.Value);
                                }
                            }
                        }
                    }

                    timer += 0.1; // interval 에 따른 조정 필요
                }

            }





            catch (Exception ex)
            {
                Debug.WriteLine($"ChartUpdate Error: {ex.Message}");
            }
        }

        public void PauseChart()
        {
            try
            {
                savedTimer = timer;
                isPausedState = true;
                Debug.WriteLine($"PostTimeChartViewModel PauseChart: timer={timer:F3} 저장, isPausedState={isPausedState}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"PostTimeChartViewModel PauseChart 오류: {ex.Message}");
            }
        }

        public void RestoreTimerFromPause()
        {
            try
            {
                Debug.WriteLine($"PostTimeChartViewModel RestoreTimerFromPause 호출: isPausedState={isPausedState}, savedTimer={savedTimer:F3}, 현재 timer={timer:F3}");

                if (isPausedState && savedTimer > 0)
                {
                    timer = savedTimer;
                    Debug.WriteLine($"PostTimeChartViewModel: timer 복원 완료 = {timer:F3}");
                }
                else
                {
                    Debug.WriteLine($"PostTimeChartViewModel: timer 복원 스킵 (isPausedState={isPausedState}, savedTimer={savedTimer:F3})");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"PostTimeChartViewModel RestoreTimerFromPause 오류: {ex.Message}");
            }
        }

        public void ResetPauseState()
        {
            try
            {
                isPausedState = false;
                Debug.WriteLine($"PostTimeChartViewModel: isPausedState 리셋");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"PostTimeChartViewModel ResetPauseState 오류: {ex.Message}");
            }
        }

        public void ClearChart()
        {
            Debug.WriteLine($"PostTimeChartViewModel ClearChart 시작");

            try
            {
                Debug.WriteLine($"  ChartYItems.Count = {ChartYItems?.Count}");

                // 1. DataSeries 데이터만 Clear
                foreach (var data in lineData.ToList())
                {
                    try { data?.Clear(); } catch { }
                }

                // 2. 렌더링 컬렉션만 Clear
                RenderableSeries.Clear();
                lineData.Clear();
                XAxes.Clear();
                YAxes.Clear();
                _seriesRefs.Clear();

                // ChartYItems는 유지 (사용자 선택 유지)

                // 3. 타이머 초기화
                timer = 0;
                savedTimer = 0;
                isPausedState = false;

                Debug.WriteLine($"PostTimeChartViewModel 차트 초기화 완료");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ClearChart 오류: {ex.Message}");
            }
        }
    }
}
