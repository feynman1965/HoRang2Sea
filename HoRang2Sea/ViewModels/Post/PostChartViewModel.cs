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
    public class PostChartViewModel : PostPanelWorkspaceViewModel
    {
        private bool _isPropertyDialogOpen;
        private string _searchKeyword;
        private ObservableCollection<string> _filteredChartGlobalItems;
        private double savedTimer = 0; // 일시정지 시 timer 저장
        private bool isPausedState = false; // 일시정지 상태 추적

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

        public PostChartViewModel(string displayName, DocumentViewModel parent = null)
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
            ChartXItems.CollectionChanged += ChartItems_CollectionChanged;
            ChartYItems.CollectionChanged += ChartItems_CollectionChanged;
        }

        private void ChartItems_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (_isPropertyDialogOpen) return;
            UpdateChartGlobalItems();
            UpdateFilteredList();
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

        // 컴포넌트 트리에서 변수를 더블클릭 → Y축 항목으로 추가 (기존 4개 제한 유지)
        public void AddYItem(string name)
        {
            if (string.IsNullOrEmpty(name)) return;
            if (ChartYItems.Contains(name)) return;
            if (ChartYItems.Count >= 4)
            {
                // 이전에는 말없이 무시했다(2026-10-05)
                System.Windows.MessageBox.Show("This chart shows up to 4 variables. Remove one first (double-click it in the Y Axis list).",
                    "Select Variables", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                return;
            }

            ChartYItems.Add(name);
            if (ChartGlobalItems != null) ChartGlobalItems.Remove(name);
            UpdateFilteredList();
        }

        // 우측 Y 목록에서 더블클릭 → 제거
        public void RemoveYItem(string name)
        {
            if (string.IsNullOrEmpty(name)) return;
            if (!ChartYItems.Contains(name)) return;

            ChartYItems.Remove(name);
            if (ChartGlobalItems != null && !ChartGlobalItems.Contains(name))
                ChartGlobalItems.Add(name);
            UpdateFilteredList();
        }

        // 트리에서 우클릭 → "X축으로 지정" (X축은 1개만, 기존 X는 목록으로 복귀)
        public void SetXItem(string name)
        {
            if (string.IsNullOrEmpty(name)) return;
            if (ChartXItems.Contains(name)) return;

            foreach (var prev in ChartXItems.ToList())
            {
                ChartXItems.Remove(prev);
                if (ChartGlobalItems != null && !ChartGlobalItems.Contains(prev))
                    ChartGlobalItems.Add(prev);
            }

            // X 모드에서 이미 Y로 추가된 변수를 X로 지정할 때 이중 등록 방지
            if (ChartYItems.Contains(name)) ChartYItems.Remove(name);

            ChartXItems.Add(name);
            if (ChartGlobalItems != null) ChartGlobalItems.Remove(name);
            UpdateFilteredList();
        }

        // 우측 X 목록에서 더블클릭 → 제거
        public void RemoveXItem(string name)
        {
            if (string.IsNullOrEmpty(name)) return;
            if (!ChartXItems.Contains(name)) return;

            ChartXItems.Remove(name);
            if (ChartGlobalItems != null && !ChartGlobalItems.Contains(name))
                ChartGlobalItems.Add(name);
            UpdateFilteredList();
        }

        private void UpdateChartGlobalItems()
        {
            if (_isPropertyDialogOpen) return;
            List<string> newItems = null;

            if (BaseMWModel is FishingBoatMW FishingBoatMW)
            {
                newItems = FishingBoatMW.FishingBoatMWOuts
                    .Where(mw => !ChartXItems.Contains(mw.Name) && !ChartYItems.Contains(mw.Name))
                    .Select(wd => wd.Name)
                    .ToList();
            }
            else if (BaseMWModel is PortGuideShipMW PortGuideShipMW)
            {
                newItems = PortGuideShipMW.PortGuideShipMWOuts
                    .Where(mw => !ChartXItems.Contains(mw.Name) && !ChartYItems.Contains(mw.Name))
                    .Select(wd => wd.Name)
                    .ToList();
            }
            else if (BaseMWModel is TrainingShipMW TrainingShipMW)
            {
                newItems = TrainingShipMW.TrainingShipMWOuts
                    .Where(mw => !ChartXItems.Contains(mw.Name) && !ChartYItems.Contains(mw.Name))
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

        public DelegateCommand OnChartPropertyCommand { get; set; }

        protected override string WorkspaceName { get { return "BottomHost"; } }

        public DocumentViewModel ParentViewModel { get; set; }
        public double timer { get; set; }

        // 인스턴스별 모드: false = 시간축 강제(y-t multi 겹침), true = X변수 사용(y-x). 기본 false(기존 동작 보존).
        public bool UseXAxisVariable { get; set; } = false;

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


        public void OnChartProperty()
        {
            _isPropertyDialogOpen = true;
            try
            {
                List<string> newItems = null;

                if (BaseMWModel is FishingBoatMW FishingBoatMW)
                {
                    newItems = FishingBoatMW.FishingBoatMWOuts
                        .Where(mw => !ChartXItems.Any(i => i == mw.Name) && !ChartYItems.Any(i => i == mw.Name))
                        .Select(wd => wd.Name)
                        .ToList();
                }
                else if (BaseMWModel is PortGuideShipMW PortGuideShipMW)
                {
                    newItems = PortGuideShipMW.PortGuideShipMWOuts
                        .Where(mw => !ChartXItems.Any(i => i == mw.Name) && !ChartYItems.Any(i => i == mw.Name))
                        .Select(wd => wd.Name)
                        .ToList();
                }
                else if (BaseMWModel is TrainingShipMW TrainingShipMW)
                {
                    newItems = TrainingShipMW.TrainingShipMWOuts
                        .Where(mw => !ChartXItems.Any(i => i == mw.Name) && !ChartYItems.Any(i => i == mw.Name))
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
                            ChartSet(true);
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

            // 일시정지 상태에서 재개되는 경우 timer 복원, 아니면 0으로 초기화
            if (isPausedState)
            {
                timer = savedTimer;
                isPausedState = false;
                Debug.WriteLine($"XYChart 일시정지에서 재개: timer 복원 = {timer:F3}");
            }
            else
            {
                timer = 0;
                Debug.WriteLine($"XYChart 새로 시작: timer = 0");
            }
            // 시뮬 중 변수 추가(backfill) 시 timer 리셋으로 새 점이 원점부터 덮여 그려지는 문제(해양대 0624) → 기록 끝 시각으로 이어감.
            double backfillLastX = -1;
            // 되채움은 기록 버퍼를 한 번 떠서 모든 변수에 같은 행을 쓴다(계산 중에 행이 늘어도 변수끼리 어긋나지 않게, 2026-10-06).
            // 되채움이 아니면 0 → 다음 갱신이 지금까지의 기록을 처음부터 그린다(일시정지 뒤 재개도 이어짐).
            List<double[]> __snap = null; int __snapRows = 0;
            if (backfill && BaseMWModel is GenericPortDllModel __snapModel) { __snap = new List<double[]>(); __snapRows = __snapModel.CopyRecordedRows(0, __snap); }
            _drawnRows = __snapRows;
            var __units = (BaseMWModel as GenericPortDllModel)?.OutputUnits() ?? new Dictionary<string, string>();

            string XAxis = "";

            //X축 0개인 경우, (Timer)
            if (!UseXAxisVariable || ChartXItems.Count == 0)
            {
                XAxis = "TimeX";
            }
            //X축 선택한 경우
            else
            {
                XAxis = ChartXItems[0] + "X";
            }

            var xNumAxis = new NumericAxisViewModel
            {
                AutoRange = AutoRange.Always,
                Id = XAxis,
                AxisAlignment = AxisAlignment.Bottom,
                AxisTitle = (XAxis == "TimeX") ? "Time [sec]" : WithUnit(__units, ChartXItems[0]),
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
                Color.FromRgb(255, 127, 14)    // Orange
            };
            int colorIdx = 0;
            foreach (var Chartitem in ChartYItems.Distinct())
            {
                string YAxis = Chartitem + "Y";
                var yNumAxis = new NumericAxisViewModel
                {
                    AutoRange = AutoRange.Always,
                    AxisAlignment = AxisAlignment.Left,
                    AxisTitle = WithUnit(__units, Chartitem),
                    // 0624 피드백 "음영이 선과 맞지 않음": 여러 Y축이 각자 밴드를 겹쳐 그리면 어떤 선과도 안 맞음 → 변수 1개일 때만 밴드.
                    DrawMajorBands = ChartYItems.Count == 1,
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

                // 시뮬 후/중 임의 변수 선택 시: 기록 버퍼에서 x-y series 되채움 (실행 시작 시엔 버퍼 비어 no-op).
                // X축이 변수면 그 변수의 기록값을, 없으면(TimeX) 시간(step*0.001)을 x로 사용. x/y 모두 매 100스텝 샘플이라 인덱스 정렬.
                if (backfill && BaseMWModel is GenericPortDllModel recModel)
                {
                    var yRec = __snap != null ? SnapSeries(__snap, recModel.RecordedColumn(Chartitem)) : recModel.GetRecordedSeries(Chartitem);
                    System.Collections.Generic.List<(double x, double y)> xRec =
                        (UseXAxisVariable && ChartXItems.Count > 0)
                            ? (__snap != null ? SnapSeries(__snap, recModel.RecordedColumn(ChartXItems[0])) : recModel.GetRecordedSeries(ChartXItems[0])) : null;
                    int n = (xRec != null) ? System.Math.Min(yRec.Count, xRec.Count) : yRec.Count;
                    for (int k = 0; k < n; k++)
                    {
                        double xv = (xRec != null) ? xRec[k].y : yRec[k].x;
                        newLineData.Append(xv, yRec[k].y);
                        if (yRec[k].x > backfillLastX) backfillLastX = yRec[k].x;   // 기록 마지막 시각 추적 (timer 이어가기용)
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
                colorIdx++;
            }

            // backfill 로 기존 기록을 채웠으면 timer를 기록 끝 시각+간격으로 복원 → 시뮬 중 변수 추가해도 시간축이 이어짐
            if (backfill && backfillLastX >= 0)
            {
                timer = backfillLastX + 0.1;
                Debug.WriteLine($"✅ backfill 후 timer 이어가기: {timer:F3}");
            }
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
            bool useX = UseXAxisVariable && ChartXItems.Count > 0;
            int xc = useX ? rec.RecordedColumn(ChartXItems[0]) : 0;
            if (useX && xc <= 0) return;   // X 변수가 기록에 없음
            var cols = new List<(XyDataSeries<double, double> s, int c)>();
            foreach (var d in lineData)
            {
                if (d == null) continue;
                int c = rec.RecordedColumn(d.SeriesName);
                if (c > 0) cols.Add((d, c));
            }
            int iv = GenericPortDllModel.RecordStepInterval;
            double lastT = -1;
            foreach (var row in _rowBuf)
            {
                int step = (int)row[0];
                if (iv < 100 && step % 100 != 0) continue;   // 차트는 0.1초 간격까지만
                double x = useX ? (xc < row.Length ? row[xc] : double.NaN) : step * 0.001;
                if (double.IsNaN(x)) continue;
                foreach (var (s, c) in cols) if (c < row.Length) s.Append(x, row[c]);
                lastT = step * 0.001;
            }
            if (lastT >= 0) timer = lastT + 0.1;
        }

        public void ChartUpdate()
        {
            // RenderableSeries가 없는데 ChartYItems가 있으면 자동으로 ChartSet() 호출
            if (RenderableSeries.Count == 0 && ChartYItems.Count > 0)
            {
                Debug.WriteLine($"XYChart: ChartSet() 자동 호출 (ChartYItems={ChartYItems.Count})");
                ChartSet(BaseMWModel is GenericPortDllModel _gm && _gm.HasRecordedData);
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
                            double xvalue = 0;

                            //X axis 미선택 시 , timer로 작동 
                            if (!UseXAxisVariable || ChartXItems.Count == 0)
                            {
                                xvalue = timer;
                            }

                            //X axis 선택 시, 해당 데이터로
                            else
                            {
                                var xdata = FishingBoatMW.FishingBoatMWOuts.FirstOrDefault(d => d.Name == ChartXItems[0]);
                                xvalue = xdata.Value;
                            }

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

                    timer += 0.1; //interval
                }

                else if (BaseMWModel is PortGuideShipMW PortGuideShipMW)
                {
                    foreach (var Chartitem in ChartYItems.Distinct())
                    {
                        var data = PortGuideShipMW.PortGuideShipMWOuts.FirstOrDefault(d => d.Name == Chartitem);
                        if (data != null)
                        {
                            double xvalue = 0;

                            //X axis 미선택 시 , timer로 작동 
                            if (!UseXAxisVariable || ChartXItems.Count == 0)
                            {
                                xvalue = timer;
                            }

                            //X axis 선택 시, 해당 데이터로
                            else
                            {
                                var xdata = PortGuideShipMW.PortGuideShipMWOuts.FirstOrDefault(d => d.Name == ChartXItems[0]);
                                xvalue = xdata.Value;
                            }

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

                    timer += 0.1; //interval
                }
                else if (BaseMWModel is TrainingShipMW TrainingShipMW)
                {
                    foreach (var Chartitem in ChartYItems.Distinct())
                    {
                        var data = TrainingShipMW.TrainingShipMWOuts.FirstOrDefault(d => d.Name == Chartitem);
                        if (data != null)
                        {
                            double xvalue = 0;

                            //X axis 미선택 시 , timer로 작동 
                            if (!UseXAxisVariable || ChartXItems.Count == 0)
                            {
                                xvalue = timer;
                            }

                            //X axis 선택 시, 해당 데이터로
                            else
                            {
                                var xdata = TrainingShipMW.TrainingShipMWOuts.FirstOrDefault(d => d.Name == ChartXItems[0]);
                                xvalue = xdata.Value;
                            }

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

                    timer += 0.1; //interval
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
                Debug.WriteLine($"PostChartViewModel PauseChart: timer={timer:F3} 저장, isPausedState={isPausedState}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"PostChartViewModel PauseChart 오류: {ex.Message}");
            }
        }

        public void RestoreTimerFromPause()
        {
            try
            {
                Debug.WriteLine($"PostChartViewModel RestoreTimerFromPause 호출: isPausedState={isPausedState}, savedTimer={savedTimer:F3}, 현재 timer={timer:F3}");

                if (isPausedState && savedTimer > 0)
                {
                    timer = savedTimer;
                    Debug.WriteLine($"PostChartViewModel: timer 복원 완료 = {timer:F3}");
                }
                else
                {
                    Debug.WriteLine($"PostChartViewModel: timer 복원 스킵 (isPausedState={isPausedState}, savedTimer={savedTimer:F3})");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"PostChartViewModel RestoreTimerFromPause 오류: {ex.Message}");
            }
        }

        public void ResetPauseState()
        {
            try
            {
                isPausedState = false;
                Debug.WriteLine($"PostChartViewModel: isPausedState 리셋");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"PostChartViewModel ResetPauseState 오류: {ex.Message}");
            }
        }

        public void ClearChart()
        {
            Debug.WriteLine($"PostChartViewModel ClearChart 시작");

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

                // ChartYItems는 유지 (사용자 선택 유지)

                // 3. 타이머 초기화
                timer = 0;
                _drawnRows = 0;   // 기록 버퍼를 다시 처음부터 그린다
                savedTimer = 0;
                isPausedState = false;

                Debug.WriteLine($"PostChartViewModel 차트 초기화 완료");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ClearChart 오류: {ex.Message}");
            }
        }
    }
}
