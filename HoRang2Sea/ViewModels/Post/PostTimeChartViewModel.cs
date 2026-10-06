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
            RemoveChartTabCommand = new DelegateCommand<string>(OnRemoveChartTab);
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

        private string _selectedChartTab;   // "전체" 제거: 기본은 첫 변수(RebuildChartTabs에서 설정)
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

        // 차트 하단 범례: 변수별 단일 표시이므로 항상 숨김(탭 이름이 곧 변수명)
        private bool _showChartLegend = false;
        public bool ShowChartLegend
        {
            get => _showChartLegend;
            set => SetValue(ref _showChartLegend, value);
        }

        // ChartYItems 기준으로 탭 목록 재구성: 변수별 탭만(오버레이 "전체" 제거)
        private void RebuildChartTabs()
        {
            ChartTabs.Clear();
            foreach (var name in ChartYItems.Distinct())
                ChartTabs.Add(name);
            HasChartTabs = ChartYItems.Count > 0;
            _selectedChartTab = ChartTabs.FirstOrDefault();   // "전체" 제거: 변수별로만, 첫 변수 선택
            RaisePropertyChanged(nameof(SelectedChartTab));
            ApplyTabView();
        }

        // 선택 탭(변수)에 해당하는 series/axis 만 표시. 데이터는 모든 series 에 계속 append 되므로 탭 전환만으로 즉시 전환된다.
        private void ApplyTabView()
        {
            string tab = _selectedChartTab;
            ShowChartLegend = false;   // 단일 변수 표시 → 범례 불필요
            foreach (var r in _seriesRefs)
            {
                bool show = !string.IsNullOrEmpty(tab) && r.name == tab;
                r.series.IsVisible = show;
                r.axis.Visibility = show ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
                // 0624 피드백 "음영이 선과 맞지 않음": 숨긴(Collapsed) 축의 음영밴드가 계속 그려져
                // 표시 중인 선과 어긋난 음영이 남음 → 표시 중인 축만 밴드를 그린다.
                if (r.axis is NumericAxisViewModel na) na.DrawMajorBands = show;
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

            // 트리 갱신: available(미추가) + added(ChartYItems, ✓표시). 검색 시 added도 동일 필터.
            var addedForTree = string.IsNullOrEmpty(SearchKeyword)
                ? ChartYItems.ToList()
                : ChartYItems.Where(n => n != null && n.IndexOf(SearchKeyword, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
            GroupedChartGlobalItems = ChartVariableGroup.Build(FilteredChartGlobalItems, addedForTree, expandAll: true);
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

        // 변수 탭의 ✕ 클릭 → 해당 변수 차트에서 제거 (트리 더블클릭 제거와 동일 경로)
        public DelegateCommand<string> RemoveChartTabCommand { get; private set; }
        private void OnRemoveChartTab(string name)
        {
            if (string.IsNullOrEmpty(name)) return;
            RemoveYItem(name);
            ChartSet(true);   // 남은 변수로 재렌더 + 탭 재구성
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
                    Caption = "OK",
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
            // 시뮬 중 변수 추가(backfill) 시 timer가 0으로 리셋되면 새 점들이 원점부터 기존 데이터 위에 덮여 그려짐(해양대 0624).
            // → backfill 후 기록의 마지막 시각으로 timer를 이어간다 (루프 뒤에서 복원).
            double backfillLastX = -1;
            // 되채움은 기록 버퍼를 한 번 떠서 모든 변수에 같은 행을 쓴다(계산 중에 행이 늘어도 변수끼리 어긋나지 않게, 2026-10-06).
            // 되채움이 아니면 0 → 다음 갱신이 지금까지의 기록을 처음부터 그린다(일시정지 뒤 재개도 이어짐).
            List<double[]> __snap = null; int __snapRows = 0;
            if (backfill && BaseMWModel is GenericPortDllModel __snapModel) { __snap = new List<double[]>(); __snapRows = __snapModel.CopyRecordedRows(0, __snap); }
            _drawnRows = __snapRows;
            var __units = (BaseMWModel as GenericPortDllModel)?.OutputUnits() ?? new Dictionary<string, string>();

            string XAxis = "Time";

            var xNumAxis = new NumericAxisViewModel
            {
                AutoRange = AutoRange.Always,
                Id = XAxis,
                AxisAlignment = AxisAlignment.Bottom,
                AxisTitle = "Time [sec]",
                DrawMajorBands = false,
                TextFormatting = "G6", CursorTextFormatting = "G6",
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
                    AxisTitle = WithUnit(__units, Chartitem),
                    DrawMajorBands = true,
                    BorderThickness = ythick,
                    TextFormatting = "G6", CursorTextFormatting = "G6",
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
                    var rec = __snap != null ? SnapSeries(__snap, recModel.RecordedColumn(Chartitem)) : recModel.GetRecordedSeries(Chartitem);
                    double ymin = double.MaxValue, ymax = double.MinValue; int cnt = 0;
                    foreach (var pt in rec)
                    {
                        newLineData.Append(pt.x, pt.y);
                        if (pt.x > backfillLastX) backfillLastX = pt.x;   // 기록 마지막 시각 추적 (timer 이어가기용)
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

            // backfill 로 기존 기록을 채웠으면 timer를 기록 끝 시각+간격으로 복원 → 시뮬 중 변수 추가해도 시간축이 이어짐
            if (backfill && backfillLastX >= 0)
            {
                timer = backfillLastX + 0.1;
                Debug.WriteLine($"✅ backfill 후 timer 이어가기: {timer:F3}");
            }

            RebuildChartTabs();
        }


        // ---- 실시간 차트 = 계산 객체의 기록 버퍼(2026-10-06) ----
        // 이전에는 갱신 이벤트(100 step)마다 timer 를 0.1초씩 올리고 그때의 최신 값을 찍어서, 화면이 밀리면 값과 시각이 어긋났다.
        // 이제 계산 스레드가 RecordStep 으로 남긴 행(step, 출력)을 그대로 그린다 — CSV · Import Data 와 같은 값 · 같은 시각.
        private int _drawnRows;
        private readonly List<double[]> _rowBuf = new List<double[]>();

        private string _chartTitleText = "";
        /// <summary>차트 제목 — 모듈 VM 이 Run 할 때 "레이아웃 · 프로파일"로 넣는다(이전에는 "Monitor Chart" 고정).</summary>
        public string ChartTitleText
        {
            get => _chartTitleText;
            set { _chartTitleText = value ?? ""; RaisePropertyChanged(nameof(ChartTitleText)); }
        }

        // 기록 스냅샷에서 변수 하나의 (시각, 값) — 되채움용. 차트는 0.1초 간격까지만(기록 간격이 더 촘촘해도).
        private static List<(double x, double y)> SnapSeries(List<double[]> rows, int col)
        {
            var res = new List<(double x, double y)>();
            if (rows == null || col <= 0) return res;
            int iv = GenericPortDllModel.RecordStepInterval < 1 ? 1 : GenericPortDllModel.RecordStepInterval;
            foreach (var r in rows)
            {
                if (col >= r.Length) continue;
                int step = (int)r[0];
                if (step % iv != 0) continue;
                if (iv < 100 && step % 100 != 0) continue;
                res.Add((step * 0.001, r[col]));
            }
            return res;
        }

        // 축 제목에 단위를 붙인다(이름에 이미 [단위]가 있거나 단위가 없으면 그대로).
        private static string WithUnit(Dictionary<string, string> units, string name)
        {
            if (string.IsNullOrEmpty(name) || units == null || name.TrimEnd().EndsWith("]")) return name;
            return units.TryGetValue(name, out var u) && !string.IsNullOrWhiteSpace(u) && u.Trim() != "-" ? $"{name} [{u.Trim()}]" : name;
        }

        private void AppendRecordedRows(GenericPortDllModel rec)
        {
            _rowBuf.Clear();
            int total = rec.CopyRecordedRows(_drawnRows, _rowBuf);
            if (total < _drawnRows)   // 새 실행이 기록을 비웠다 → 처음부터
            {
                foreach (var d in lineData) d?.Clear();
                _drawnRows = 0;
                _rowBuf.Clear();
                total = rec.CopyRecordedRows(0, _rowBuf);
            }
            _drawnRows = total;
            if (_rowBuf.Count == 0) return;
            var cols = new List<(XyDataSeries<double, double> s, int c)>();
            foreach (var d in lineData)
            {
                if (d == null) continue;
                int c = rec.RecordedColumn(d.SeriesName);
                if (c > 0) cols.Add((d, c));
            }
            int iv = GenericPortDllModel.RecordStepInterval;
            double lastX = -1;
            foreach (var row in _rowBuf)
            {
                int step = (int)row[0];
                if (iv < 100 && step % 100 != 0) continue;   // 차트는 0.1초 간격까지만
                double x = step * 0.001;
                foreach (var (s, c) in cols) if (c < row.Length) s.Append(x, row[c]);
                lastX = x;
            }
            if (lastX >= 0) timer = lastX + 0.1;   // 일시정지 저장 · 되채움과 같은 기준
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


            // 계산 객체의 기록 버퍼에서 그린다(2026-10-06). 아래 모델별 분기(timer += 0.1)는 기록이 없는 모델(Nexo)에만 남는다.
            if (BaseMWModel is GenericPortDllModel __rec) { AppendRecordedRows(__rec); return; }
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

        // 축 자동범위 토글: 사용자 줌/팬 시 false(수동 고정 — 스냅백 방지), Auto 버튼으로 복귀.
        public void SetAxesAutoRange(bool auto)
        {
            var mode = auto ? AutoRange.Always : AutoRange.Never;
            foreach (var ax in XAxes.OfType<NumericAxisViewModel>()) ax.AutoRange = mode;
            foreach (var ax in YAxes.OfType<NumericAxisViewModel>()) ax.AutoRange = mode;
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
                _drawnRows = 0;   // 기록 버퍼를 다시 처음부터 그린다
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
