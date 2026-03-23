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
                            ChartSet();
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

        public void ChartSet()
        {
            if (System.Windows.Application.Current?.Dispatcher.CheckAccess() == false)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() => ChartSet());
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
                AxisTitle = XAxis,
                DrawMajorBands = false,
                TextFormatting = "0.00#",
                VisibleRange = new DoubleRange(0, 1000),
                BorderBrush = new SolidColorBrush(Colors.CadetBlue)
            };
            XAxes.Add(xNumAxis);

            Thickness ythick = new Thickness(0, 0, 1, 0);

            foreach (var Chartitem in ChartYItems.Distinct())
            {
                string YAxis = Chartitem + "Y";
                var yNumAxis = new NumericAxisViewModel
                {
                    AutoRange = AutoRange.Always,
                    AxisTitle = YAxis,
                    DrawMajorBands = true,
                    BorderThickness = ythick,
                    TextFormatting = "0.0#",
                    Id = YAxis,
                };
                YAxes.Add(yNumAxis);

                XyDataSeries<double, double> newLineData = new XyDataSeries<double, double>();
                newLineData.SeriesName = Chartitem;
                newLineData.AcceptsUnsortedData = true;
                // FifoCapacity 제거: 데이터가 0부터 쌓이도록 함 (롤링 윈도우 비활성화)
                lineData.Add(newLineData);

                Random random = new Random();
                Color randomColor = Color.FromArgb(255, Convert.ToByte(random.Next(256)), Convert.ToByte(random.Next(256)), Convert.ToByte(random.Next(256)));

                var newRenderableSeries = new LineRenderableSeriesViewModel
                {
                    StrokeThickness = 3,
                    Stroke = randomColor,
                    DataSeries = newLineData,
                    YAxisId = YAxis,
                    XAxisId = XAxis,
                };

                RenderableSeries.Add(newRenderableSeries);
            }
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

                    timer += 0.001; // interval 에 따른 조정 필요
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
                    timer += 0.001; // interval 에 따른 조정 필요
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

                    timer += 0.001; // interval 에 따른 조정 필요
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
