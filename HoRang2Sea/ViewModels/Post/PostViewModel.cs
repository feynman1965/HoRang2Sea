using DevExpress.Mvvm;
using DevExpress.Xpf.Core.DragDrop.Native;
using HoRang2Sea.Models;
using HoRang2Sea.Services;
using Microsoft.Win32;
using SciChart.Charting.Model.ChartSeries;
using SciChart.Charting.Model.DataSeries;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Data;
using System.Windows.Forms;

namespace HoRang2Sea.ViewModels
{
    public class PostViewModel : DocumentViewModel
    {

        public BaseModel BaseMWModel
        {
            get => GetValue<BaseModel>();
            set
            {
                SetValue(value);
                value.DataReceived += Udp_GridUpdate;
                value.DataReceived += Udp_ChartUpdate;
            }
        }
        #region construction
        public PostViewModel()
        {
            Items = new ObservableCollection<string>();
            GlobalItems = new ObservableCollection<string>();
            FilteredListSource = new ObservableCollection<string>(GlobalItems);

            // Items 컬렉션 변경 감지 이벤트 구독
            Items.CollectionChanged += Items_CollectionChanged;

            PostWorkspaces = new ObservableCollection<PostPanelWorkspaceViewModel>
            {
                new PostGridViewModel("PostGrid"),
                new PostChartViewModel("PostChart (XY)", this)
            };

            // listsource 및 Source 초기화
            OnUseRealTimeSource();

            UpdateFilteredList();
        }
        protected virtual ISaveLoadLayoutService SaveLoadLayoutService { get { return null; } }

        #endregion
        #region property
        public ObservableCollection<PostPanelWorkspaceViewModel> PostWorkspaces { get; set; }

        public ObservableCollection<string> Items { get; set; }
        public ObservableCollection<string> GlobalItems { get; set; }



        private string _searchKeyword;
        private ObservableCollection<string> _filteredListSource;

        private void UpdateFilteredList()
        {
            if (string.IsNullOrEmpty(SearchKeyword))
            {
                FilteredListSource = new ObservableCollection<string>(GlobalItems);
            }
            else
            {
                var filtered = GlobalItems
                    .Where(item => item.IndexOf(SearchKeyword, StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToList();

                FilteredListSource = new ObservableCollection<string>(filtered);
            }
            RaisePropertyChanged(nameof(FilteredListSource));
        }

        // 검색어
        public string SearchKeyword
        {
            get => _searchKeyword;
            set
            {
                SetValue(ref _searchKeyword, value);
                UpdateFilteredList();
            }
        }


        public ObservableCollection<string> FilteredListSource
        {
            get => _filteredListSource;
            set => SetValue(ref _filteredListSource, value);
        }



        private object _source;
        public virtual object Source
        {
            get => _source;
            set => SetValue(ref _source, value);
        }

        protected ObservableCollection<GridData> listsource;


        #endregion

        protected override void OnDispose()
        {
            if (BaseMWModel != null)
            {
                BaseMWModel.DataReceived -= Udp_GridUpdate;
                BaseMWModel.DataReceived -= Udp_ChartUpdate; // 추가: ChartUpdate 이벤트도 해제
            }
            base.OnDispose();
        }
        protected void OnUseRealTimeSource()
        {
            if (PostWorkspaces[0] is PostGridViewModel postGridViewModel)
            {
                listsource = postGridViewModel.listsource;
                Source = postGridViewModel.Source;
            }
            else
            {
                // PostWorkspaces[0]이 PostGridViewModel이 아닌 경우 처리
                System.Diagnostics.Debug.WriteLine("PostWorkspaces[0] is not a PostGridViewModel");
            }
        }
        public void OnGridProperty()
        {
            if (BaseMWModel is FishingBoatMW FishingBoatMW)
            {
                GlobalItems = new ObservableCollection<string>(FishingBoatMW.FishingBoatMWOuts.Where(mw => !Items.Any(i => i == mw.Name)).Select(wd => wd.Name).ToList());
            }
            else if (BaseMWModel is PortGuideShipMW PortGuideShipMW)
            {
                GlobalItems = new ObservableCollection<string>(PortGuideShipMW.PortGuideShipMWOuts.Where(mw => !Items.Any(i => i == mw.Name)).Select(wd => wd.Name).ToList());
            }

            else if (BaseMWModel is TrainingShipMW TrainingShipMW)
            {
                GlobalItems = new ObservableCollection<string>(TrainingShipMW.TrainingShipMWOuts.Where(mw => !Items.Any(i => i == mw.Name)).Select(wd => wd.Name).ToList());
            }

            // GlobalItems 업데이트 후 FilteredListSource 업데이트
            UpdateFilteredList();


            UICommand rOkCommand = new UICommand()
            {
                Caption = "Ok",
                IsDefault = true,
                Command = new DelegateCommand(() =>
                {
                    try
                    {
                        Udp_GridUpdate(null, null);
                    }
                    catch
                    {

                    };
                }
                )
            };
            UICommand rCloseCommand = new UICommand()
            {
                Caption = "Close",
                IsDefault = true,
            };
            IDialogService service = this.GetService<IDialogService>("GridPropertyDialogService");
            UICommand result = service.ShowDialog(
                dialogCommands: new[] { rOkCommand, rCloseCommand },
                title: "Import Data",
                viewModel: this
            );
        }



        // Items 컬렉션의 변경을 처리하는 메서드
        private void Items_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            // Items 컬렉션이 변경될 때 GlobalItems와 FilteredListSource를 업데이트
            UpdateGlobalItems();
            UpdateFilteredList();

            // PostGrid 업데이트
            Udp_GridUpdate(null, null);
        }

        private void UpdateGlobalItems()
        {
            if (BaseMWModel is FishingBoatMW FishingBoatMW)
            {
                GlobalItems = new ObservableCollection<string>(FishingBoatMW.FishingBoatMWOuts.Where(mw => !Items.Any(i => i == mw.Name)).Select(wd => wd.Name).ToList());
            }
            else if (BaseMWModel is PortGuideShipMW PortGuideShipMW)
            {
                GlobalItems = new ObservableCollection<string>(PortGuideShipMW.PortGuideShipMWOuts.Where(mw => !Items.Any(i => i == mw.Name)).Select(wd => wd.Name).ToList());
            }

            else if (BaseMWModel is TrainingShipMW TrainingShipMW)
            {
                GlobalItems = new ObservableCollection<string>(TrainingShipMW.TrainingShipMWOuts.Where(mw => !Items.Any(i => i == mw.Name)).Select(wd => wd.Name).ToList());
            }

        }



        public void OnAddChart()
        {
            PostWorkspaces.Add(new PostChartViewModel("PostChart (XY)", this));
        }

        public void OnAddTimeChart()
        {
            PostWorkspaces.Add(new PostTimeChartViewModel("PostChart (Time)", this));
        }
        public void OnAddGauge()
        {
            PostWorkspaces.Add(new PostGaugeViewModel("Gauge", this));
        }

        public void OnReport()
        {
            FolderBrowserDialog folder = new FolderBrowserDialog();
            string FolderPath = "";
            if (folder.ShowDialog() == DialogResult.OK)
            {
                FolderPath = folder.SelectedPath;
            }

            else { return; }

            foreach (var post in PostWorkspaces)
            {
                if (post is PostChartViewModel)
                {
                    var postChartViewModel = (PostChartViewModel)post;

                    foreach (var renderableSeries in postChartViewModel.RenderableSeries)
                    {
                        //var watch = System.Diagnostics.Stopwatch.StartNew();
                        var Chart = (LineRenderableSeriesViewModel)renderableSeries;

                        var YaxisName = Chart.YAxisId;
                        var XaxisName = Chart.XAxisId;
                        var ChartData = (XyDataSeries<double, double>)Chart.DataSeries;
                        var ChartName = ChartData.SeriesName;

                        int length = ChartData.Count;
                        //double[,] dataArray = new double[length, 2];

                        /* for (int i = 0; i < length; i++)
                         {


                             dataArray[i, 0] = x; //x데이터 -> 0,0 1,0 2,0 
                             dataArray[i, 1] = y; //y데이터 -> 0,1 1,1 2,1 
                         }*/

                        string csvFilePath = FolderPath + "\\" + XaxisName + YaxisName + "result.csv";

                        using (StreamWriter writer = new StreamWriter(csvFilePath, false))
                        {
                            string row = XaxisName + "," + YaxisName;
                            writer.WriteLine(row);
                            row = "";
                            for (int j = 0; j < length; j++)
                            {
                                double x = ChartData.XValues[j];
                                double y = ChartData.YValues[j];
                                row = x.ToString() + "," + y.ToString();
                                writer.WriteLine(row);
                            }
                            //watch.Stop();
                            //var elapsedMs = watch.ElapsedMilliseconds;
                            //MessageBox.Show(elapsedMs.ToString());

                            //var watch2 = System.Diagnostics.Stopwatch.StartNew();


                            //watch2.Stop();
                            //var elapsedMs2 = watch2.ElapsedMilliseconds;
                            //MessageBox.Show(elapsedMs2.ToString());
                        }

                    }
                }
                else if (post is PostTimeChartViewModel)
                {
                    var postChartViewModel = (PostTimeChartViewModel)post;

                    foreach (var renderableSeries in postChartViewModel.RenderableSeries)
                    {
                        //var watch = System.Diagnostics.Stopwatch.StartNew();
                        var Chart = (LineRenderableSeriesViewModel)renderableSeries;

                        var YaxisName = Chart.YAxisId;
                        var XaxisName = Chart.XAxisId;
                        var ChartData = (XyDataSeries<double, double>)Chart.DataSeries;
                        var ChartName = ChartData.SeriesName;

                        int length = ChartData.Count;

                        string csvFilePath = FolderPath + "\\" + XaxisName + YaxisName + "result.csv";

                        using (StreamWriter writer = new StreamWriter(csvFilePath, false))
                        {
                            string row = XaxisName + "," + YaxisName;
                            writer.WriteLine(row);
                            row = "";

                            for (int j = 0; j < length; j++)
                            {
                                double x = ChartData.XValues[j];
                                double y = ChartData.YValues[j];
                                row = x.ToString() + "," + y.ToString();
                                writer.WriteLine(row);
                            }
                        }

                    }
                }
            }
        }


        public void OnSaveLayout()
        {
            const string filter = "Configuration (*.xml)|*.xml|All files (*.*)|*.*";

            Microsoft.Win32.SaveFileDialog saveFileDialog = new Microsoft.Win32.SaveFileDialog() { Filter = filter };
            var saveResult = saveFileDialog.ShowDialog();
            if (saveResult.HasValue && saveResult.Value)
            {
                SaveLoadLayoutService.SaveLayout(saveFileDialog.FileName);
                FilePath = solutionItem.FilePath = saveFileDialog.FileName;
            }

        }

        public void OnImportLayout()
        {
            const string filter = "Configuration (*.xml)|*.xml|All files (*.*)|*.*";

            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog() { Filter = filter };
            var openResult = openFileDialog.ShowDialog();
            if (openResult.HasValue && openResult.Value)
            {
                SaveLoadLayoutService.LoadLayout(openFileDialog.FileName);
                FilePath = solutionItem.FilePath = openFileDialog.FileName;
            }

        }
        private void Udp_GridUpdate(object sender, DataReceivedEventArgs e)
        {
            try
            {
                if (listsource == null) return;


                if (BaseMWModel is FishingBoatMW FishingBoatMW)
                {
                    foreach (var item in Items)
                    {
                        var data = FishingBoatMW.FishingBoatMWOuts.FirstOrDefault(d => d.Name == item);
                        if (data != null)
                        {
                            if (listsource.Any(l => l.Name == data.Name))
                            {
                                var selecteddata = listsource.Single(l => l.Name == data.Name);
                                selecteddata.UpdateInternal(data.Value);

                            }
                            else
                            {
                                listsource.Add(new(data.Name, "", data.Value, data.Unit));
                            }
                        }
                    }
                }
                else if (BaseMWModel is PortGuideShipMW PortGuideShipMW)
                {
                    foreach (var item in Items)
                    {
                        var data = PortGuideShipMW.PortGuideShipMWOuts.FirstOrDefault(d => d.Name == item);
                        if (data != null)
                        {
                            if (listsource.Any(l => l.Name == data.Name))
                            {
                                var selecteddata = listsource.Single(l => l.Name == data.Name);
                                selecteddata.UpdateInternal(data.Value);

                            }
                            else
                            {
                                listsource.Add(new(data.Name, "", data.Value, data.Unit));
                            }
                        }
                    }
                }
 
                else if (BaseMWModel is TrainingShipMW TrainingShipMW)
                {
                    foreach (var item in Items)
                    {
                        var data = TrainingShipMW.TrainingShipMWOuts.FirstOrDefault(d => d.Name == item);
                        if (data != null)
                        {
                            if (listsource.Any(l => l.Name == data.Name))
                            {
                                var selecteddata = listsource.Single(l => l.Name == data.Name);
                                selecteddata.UpdateInternal(data.Value);

                            }
                            else
                            {
                                listsource.Add(new(data.Name, "", data.Value, data.Unit));
                            }
                        }
                    }
                }

            }
            catch (Exception ex)
            {
            }

        }

        private void Udp_ChartUpdate(object sender, DataReceivedEventArgs e)
        {
            ChartUpdate();
        }

        public void ChartUpdate()
        {
            for (int i = 1; i < PostWorkspaces.Count; i++)
            {
                if (PostWorkspaces[i] is PostChartViewModel)
                {
                    var postChartViewModel = (PostChartViewModel)PostWorkspaces[i];
                    postChartViewModel.ChartUpdate();
                }
                else if (PostWorkspaces[i] is PostTimeChartViewModel)
                {
                    var postTimeChartViewModel = (PostTimeChartViewModel)PostWorkspaces[i];
                    postTimeChartViewModel.ChartUpdate();
                }
                /* else if(PostWorkspaces[i] is PostGaugeViewModel)
                 {
                     var gaugeChartViewModel = (PostGaugeViewModel)PostWorkspaces[i];
                     gaugeChartViewModel.DataUpdate();
                 }*/
            }
        }



    }

    public class GridData : BindableBase
    {
        public GridData(string name, string subName, double value, string unit)
        {
            Name = name;
            SubName = subName;
            Value = Max = Min = value;
            Unit = unit;
            RaisePropertyChanged("");
        }
        public string Name { get; private set; }
        public string SubName { get; private set; }
        public double Value { get; private set; }
        public double Max { get; private set; }
        public double Min { get; private set; }
        public string Unit { get; private set; }


        public void UpdateInternal(double value)
        {
            Value = value;
            Max = Math.Max(Max, Value);
            Min = Math.Min(Min, Value);
            RaisePropertyChanged("");
        }
    }

    public class BoolToValueConverter : IValueConverter
    {
        public object TrueValue { get; set; }

        public object FalseValue { get; set; }

        public object TrueSLValue { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool flag = false;
            if (value is bool)
            {
                flag = (bool)value;
            }

            if (value is int)
            {
                flag = (int)value >= 1;
            }

            if (!flag)
            {
                return FalseValue;
            }

            return TrueValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }


}
