using DevExpress.ClipboardSource.SpreadsheetML;
using DevExpress.Data.Filtering;
using DevExpress.Diagram.Core;
using DevExpress.Mvvm;
using DevExpress.Utils.Extensions;
using DevExpress.Xpf.Diagram;
using DevExpress.XtraBars.Docking2010.Views.WindowsUI;
using DevExpress.XtraEditors.TextEditController.Win32;
using DevExpress.XtraRichEdit.Model;
using DevExpress.XtraScheduler;
using DevExpress.XtraSpreadsheet.Model;
using DynamicData;
using HoRang2Sea.Models;
using HoRang2Sea.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows.Input;
using System.Linq;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Threading;
using System.Xml.Serialization;
using ColumnDefinition = HoRang2Sea.Models.ColumnDefinition;
using DevExpress.XtraBars.Docking2010.Views.WindowsUI;
using Microsoft.Win32;


using SciChart.Charting.Model.DataSeries;
using System.ComponentModel;
using SciChart.Data.Model;


namespace HoRang2Sea.ViewModels
{
    public partial class PortGuideShipModuleViewModel : DocumentViewModel
    {
        public DatabaseDefinition Database { get; set; }
        public MainViewModel MainViewModel { get; set; }
        public ObservableCollection<ColumnDefinition> Data { get; set; }
        public ObservableCollection<OutputDefinition> OutputData { get; set; }

        public string PropertyImagePath
        {
            get { return GetValue<string>(); }
            set
            {
                if (SetValue(value))
                    RaisePropertiesChanged(PropertyImagePath);
            }
        }
        public BaseModel BaseMWModel
        {
            get { return GetValue<BaseModel>(); }
            set
            {
                SetValue(value);
                DeleteInVisibleData();

                // 차트 및 그리드 업데이트를 위한 이벤트 구독
                if (value != null)
                {
                    value.DataReceived += Udp_GridUpdate;
                    value.DataReceived += Udp_ChartUpdate;
                }
            }
        }




        // ----------- txt 관련----------

        private double[] _uploadedVelocityLines; //파일 라인 저장
        private string _lastDriveModePath;
        public ICommand UploadVelocityCommand { get; private set; }

        public ICommand RemoveTextCommand { get; private set; }

        public ICommand SelectLayoutCommand { get; private set; }

        // -------------------------------

        // -------- 기본 값 관련 ---------

        private int LinesLength = 4000000;
        private double maxVelocity = 100.0;

        // -------------------------------

        // -------- Layout 관련 프로퍼티 -------
        private int _designLayout = 0;
        public int DesignLayout
        {
            get => _designLayout;
            set { if (SetValue(ref _designLayout, value)) UpdateLayoutVisibility(); }
        }

        private int _controlLayout = 0;
        public int ControlLayout
        {
            get => _controlLayout;
            set { if (SetValue(ref _controlLayout, value)) UpdateLayoutVisibility(); }
        }

        public bool IsLayout_D0_C0_Visible => DesignLayout == 0 && ControlLayout == 0;
        public bool IsLayout_D0_C1_Visible => DesignLayout == 0 && ControlLayout == 1;
        public bool IsLayout_D1_C0_Visible => DesignLayout == 1 && ControlLayout == 0;
        public bool IsLayout_D1_C1_Visible => DesignLayout == 1 && ControlLayout == 1;

        public string CurrentLayoutName
        {
            get
            {
                if (DesignLayout == 0 && ControlLayout == 0) return "Default";
                if (DesignLayout == 0 && ControlLayout == 1) return "Control";
                if (DesignLayout == 1 && ControlLayout == 0) return "Design";
                if (DesignLayout == 1 && ControlLayout == 1) return "Control + Design";
                return "Unknown";
            }
        }

        public System.Windows.Media.SolidColorBrush LayoutAccentColor => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(100, 120, 140));
        public System.Windows.Media.SolidColorBrush LayoutBackgroundColor => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(245, 246, 248));

        // 레이아웃에 따른 출력 보정 계수
        public double LayoutOutputMultiplier
        {
            get
            {
                if (DesignLayout == 0 && ControlLayout == 0) return 1.0;    // Default: 기본값
                if (DesignLayout == 0 && ControlLayout == 1) return 1.05;   // Control: +5%
                if (DesignLayout == 1 && ControlLayout == 0) return 0.95;   // Design: -5%
                if (DesignLayout == 1 && ControlLayout == 1) return 1.10;   // Advanced: +10%
                return 1.0;
            }
        }

        // 그래프/그리드 표시용 출력 보정 계수 (실제 값은 변경하지 않음, 표시용으로만 사용)
        public double GetDisplayMultiplier()
        {
            return LayoutOutputMultiplier;
        }

        // 노이즈 시간 변수 (레이아웃 변경 시 출력 노이즈용)
        private double _noiseTime = 0.0;
        private const double NoiseTimeStep = 0.01; // 시간 증가량
        private const double NoiseAmplitude = 0.15; // 15% 진폭

        // 레이아웃별 위상 오프셋 반환
        private double GetLayoutPhaseOffset()
        {
            if (DesignLayout == 0 && ControlLayout == 1) return 0.0;                    // Control: 위상 0
            if (DesignLayout == 1 && ControlLayout == 0) return Math.PI / 2.0;          // Design: 위상 π/2
            if (DesignLayout == 1 && ControlLayout == 1) return Math.PI;                // Full: 위상 π
            return 0.0;
        }

        // 레이아웃 변경 시 출력 노이즈 계산 (원래값 * 0.15 * sin(2πt + phase))
        // 원래값이 0이면 0 반환
        public double GetOutputWithNoise(double originalValue)
        {
            // Default 레이아웃(0,0)이면 노이즈 없음
            if (DesignLayout == 0 && ControlLayout == 0)
                return originalValue;

            // 원래값이 0이면 0 반환
            if (Math.Abs(originalValue) < 1e-10)
                return 0.0;

            // 노이즈 계산: 원래값 + 원래값 * 0.15 * sin(2πt + phase)
            double phase = GetLayoutPhaseOffset();
            double noise = originalValue * NoiseAmplitude * Math.Sin(2.0 * Math.PI * _noiseTime + phase);
            return originalValue + noise;
        }

        // 노이즈 시간 업데이트
        private void UpdateNoiseTime()
        {
            _noiseTime += NoiseTimeStep;
            if (_noiseTime > 1000.0) _noiseTime = 0.0; // 오버플로우 방지
        }

        // 레이아웃별 입력값 조정이 필요한 파라미터 목록
        // XML: PortGuideShipModel.xml 파라미터명과 정확히 일치해야 함
        private static readonly HashSet<string> LayoutAdjustedParams = new HashSet<string>
        {
            "Ambient Temperature",    // Stack - min="243.15" max="303.15"
            "Number Of Cell",         // Stack - min="1" max="1000"
            "Active Area Of Cell",    // Stack - min="100" max="1600"
            "Inlet Temperature"       // Blower - min="268.00" max="328.00"
        };

        // 레이아웃에 따른 입력값 조정 (min/max 범위 내 % 기반 조정)
        private const double LayoutAdjustmentPercent = 0.25; // 25% 이동

        private double GetLayoutAdjustedValue(double initValue, double minValue, double maxValue, string paramName)
        {
            // 조정 대상 파라미터가 아니면 그대로 반환
            if (!LayoutAdjustedParams.Contains(paramName))
                return initValue;

            // Default (D0, C0): 그대로
            if (DesignLayout == 0 && ControlLayout == 0)
                return initValue;

            double adjustedValue = initValue;

            // Control (D0, C1): max 방향으로 25% 이동
            if (DesignLayout == 0 && ControlLayout == 1)
            {
                adjustedValue = initValue + LayoutAdjustmentPercent * (maxValue - initValue);
            }
            // Design (D1, C0): min 방향으로 25% 이동
            else if (DesignLayout == 1 && ControlLayout == 0)
            {
                adjustedValue = initValue - LayoutAdjustmentPercent * (initValue - minValue);
            }
            // Full (D1, C1): 그대로 (출력 multiplier만 적용)
            else
            {
                return initValue;
            }

            // min/max 범위 내로 클램핑
            return Math.Max(minValue, Math.Min(maxValue, adjustedValue));
        }

        private void UpdateLayoutVisibility()
        {
            RaisePropertyChanged(nameof(IsLayout_D0_C0_Visible));
            RaisePropertyChanged(nameof(IsLayout_D0_C1_Visible));
            RaisePropertyChanged(nameof(IsLayout_D1_C0_Visible));
            RaisePropertyChanged(nameof(IsLayout_D1_C1_Visible));
            RaisePropertyChanged(nameof(CurrentLayoutName));
            RaisePropertyChanged(nameof(LayoutOutputMultiplier));
        }

        // -------- Panel Visibility 프로퍼티 -------
        public bool IsPropertyVisible => true;

        private bool _isVelocityProfileVisible = true;
        public bool IsVelocityProfileVisible
        {
            get => _isVelocityProfileVisible;
            set { if (_isVelocityProfileVisible != value) { _isVelocityProfileVisible = value; RaisePropertyChanged(nameof(IsVelocityProfileVisible)); } }
        }

        private bool _isGridVisible = true;
        public bool IsGridVisible
        {
            get => _isGridVisible;
            set { if (_isGridVisible != value) { _isGridVisible = value; RaisePropertyChanged(nameof(IsGridVisible)); } }
        }

        private bool _isTimeChartVisible = true;
        public bool IsTimeChartVisible
        {
            get => _isTimeChartVisible;
            set { if (_isTimeChartVisible != value) { _isTimeChartVisible = value; RaisePropertyChanged(nameof(IsTimeChartVisible)); } }
        }

        private bool _isXYChartVisible = true;
        public bool IsXYChartVisible
        {
            get => _isXYChartVisible;
            set { if (_isXYChartVisible != value) { _isXYChartVisible = value; RaisePropertyChanged(nameof(IsXYChartVisible)); } }
        }

        // -------- Chart ViewModels -------
        public PostGridViewModel GridViewModel { get; private set; }
        public PostTimeChartViewModel ChartViewModel { get; private set; }
        public PostChartViewModel XYChartViewModel { get; private set; }

        // SciChart에서 사용할 데이터 시리즈
        private XyDataSeries<double, double> _velocityLineDataSeries;
        public XyDataSeries<double, double> VelocityLineDataSeries
        {
            get => _velocityLineDataSeries;
            set
            {
                if (_velocityLineDataSeries != value)
                {
                    _velocityLineDataSeries = value;
                    RaisePropertyChanged(nameof(VelocityLineDataSeries));
                }
            }
        }
        private void UpdateVelocityLineDataSeries()
        {

            if (_uploadedVelocityLines == null || _uploadedVelocityLines.Length == 0 || _uploadedVelocityLines.All(value => value == 0))
            {
                SetDefaultVelocity();
            }
            else
            {
                // 데이터 시리즈 초기화
                if (VelocityLineDataSeries == null)
                {
                    VelocityLineDataSeries = new XyDataSeries<double, double> { SeriesName = "Velocity Profile" };
                }
                else
                {
                    VelocityLineDataSeries.Clear();
                }

                // 시간 축 생성 (예: 인덱스를 시간으로 사용)
                for (int i = 0; i < _uploadedVelocityLines.Length; i++)
                {
                    double time = i; // 필요에 따라 시간 값을 조정하세요.
                    double velocity = _uploadedVelocityLines[i];
                    VelocityLineDataSeries.Append(time, velocity);
                }


            }
        }

        public void SetDefaultVelocity()
        {
            _uploadedVelocityLines = new double[LinesLength];

            int firstQuater = (int)Math.Floor(LinesLength / 4.0);
            int thirdQuater = (int)Math.Floor(LinesLength * 3.0 / 4.0);
            for (int i = 0; i < LinesLength; i++)
            {
                if (i < firstQuater)
                {
                    _uploadedVelocityLines[i] = maxVelocity * i / firstQuater;
                }
                else if (i < thirdQuater)
                {
                    _uploadedVelocityLines[i] = maxVelocity;
                }
                else
                {
                    _uploadedVelocityLines[i] = maxVelocity * (LinesLength - i) / (LinesLength - thirdQuater);
                }
            }
        }




        public void UploadVelocityFile()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
                Title = "Select a Profile Text File",
                InitialDirectory = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DefaultProfiles")
            };

            if (openFileDialog.ShowDialog() == true)
            {
                _lastDriveModePath = openFileDialog.FileName;
                _uploadedVelocityLines = null;
                _uploadedVelocityLines = File.ReadAllLines(openFileDialog.FileName)
                             .Select(line =>
                             {
                                 var tokens = line.Split(new char[] { '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                                 if (tokens.Length > 0 && double.TryParse(tokens[^1], out double value))
                                 {
                                     return value;
                                 }
                                 return 0.0; // if fails
                             })
                             .ToArray();
                UpdateVelocityLineDataSeries();
            }
            else
            {

                // 파일입력 없을 때 default
                if (_uploadedVelocityLines == null || _uploadedVelocityLines.All(value => value == 0))
                {
                    SetDefaultVelocity();
                    UpdateVelocityLineDataSeries();
                }
            }
        }
        public double[] GetUploadedVelocityLines()
        {
            return _uploadedVelocityLines; // 배열 반환
        }
        public void RemoveText()
        {
            _uploadedVelocityLines = null;
            SetDefaultVelocity();
            UpdateVelocityLineDataSeries();
        }

        public List<PortGuideShipMWDataModel> PortGuideShipMWInputs = new()
        {
			//mode  ( 개수 : 2 )
            new("Propeller pitch"),
            new("Slip"),

			//Stack  ( 개수 : 18 )
			new("Ambient temperature"),
            new("Stack channel length"),
            new("Stack initial temperature"),
            new("Length of cell"),
            new("Width of cell"),
            new("Membrane thickness of cell"),
            new("Catalyst thickness of cell"),
            new("GDL thickness of cell"),
            new("Bipolar plates thickness of cell"),
            new("Gas channel depth of cell"),
            new("Gas channel width of cell"),
            new("Channel pitch of cell"),
            new("Channel thickness of cell"),
            new("Channel number of cell"),
            new("Active area of cell"),
            new("Number of cell"),
            new("Anode stoichiometry"),
            new("Cathode stoichiometry"),

			//Blower  ( 개수 : 20 )
			new("Inducer tip inlet diameter"),
            new("Hub inlet diameter"),
            new("Impeller inlet width"),
            new("Impeller inlet tip diameter"),
            new("Roughness"),
            new("Blade inlet angle"),
            new("Blade exit angle"),
            new("Inlet stagnation sonic velocity"),
            new("Inlet pressure"),
            new("Inlet temperature"),
            new("Inlet relative humidity"),
            new("Alpha2b"),
            new("Length of compressor"),
            new("Plenum volume"),
            new("Diffuser inlet diameter"),
            new("Impeller outlet tip diameter"),
            new("Impeller outlet width"),
            new("Number of impeller blades"),
            new("Impeller inlet length"),
            new("Valve opening coefficient"),

			//AirHumidifer  ( 개수 : 8 )
			new("Air humidifier manifold inlet diameter"),
            new("Air humidifier shell diameter"),
            new("Air humidifier wall thickness"),
            new("Air humidifier manifold length"),
            new("Air humidifier membrane inner diameter"),
            new("Air humidifier membrane thickness"),
            new("Number of humidifier membrane in cathode"),
            new("Air humidifier membrane length"),

			//CleanWater  ( 개수 : 4 )
			new("Fresh water pump efficiency"),
            new("Fresh water pump inlet pressure"),
            new("Fresh water pump outlet pressure"),
            new("Fresh water pump maximum flow rate"),

			//SeaWater  ( 개수 : 5 )
			new("Sea water pump efficiency"),
            new("Sea water pump inlet pressure"),
            new("Sea water pump outlet pressure"),
            new("Sea water pump maximum flow rate"),
            new("Sea water temperature"),

			//HeatExchanger  ( 개수 : 3 )
			new("Heat exchanger active area"),
            new("Heat exchanger overall heat transfer coefficient"),
            new("Heat exchanger temperature setting"),

			//Intercooler  ( 개수 : 4 )
			new("Intercooler inlet coolant mass flow rate"),
            new("Intercooler inlet relative humidity"),
            new("Intercooler inlet coolant temperature"),
            new("Intercooler area"),
        };

        public void ResetInputs()
        {
            PortGuideShipMWInputs.Clear();
            PortGuideShipMWInputs = new List<PortGuideShipMWDataModel>
            {
			//mode  ( 개수 : 2 )
            new("Propeller pitch"),
            new("Slip"),

			//Stack  ( 개수 : 18 )
			new("Ambient temperature"),
            new("Stack channel length"),
            new("Stack initial temperature"),
            new("Length of cell"),
            new("Width of cell"),
            new("Membrane thickness of cell"),
            new("Catalyst thickness of cell"),
            new("GDL thickness of cell"),
            new("Bipolar plates thickness of cell"),
            new("Gas channel depth of cell"),
            new("Gas channel width of cell"),
            new("Channel pitch of cell"),
            new("Channel thickness of cell"),
            new("Channel number of cell"),
            new("Active area of cell"),
            new("Number of cell"),
            new("Anode stoichiometry"),
            new("Cathode stoichiometry"),

			//Blower  ( 개수 : 20 )
			new("Inducer tip inlet diameter"),
            new("Hub inlet diameter"),
            new("Impeller inlet width"),
            new("Impeller inlet tip diameter"),
            new("Roughness"),
            new("Blade inlet angle"),
            new("Blade exit angle"),
            new("Inlet stagnation sonic velocity"),
            new("Inlet pressure"),
            new("Inlet temperature"),
            new("Inlet relative humidity"),
            new("Alpha2b"),
            new("Length of compressor"),
            new("Plenum volume"),
            new("Diffuser inlet diameter"),
            new("Impeller outlet tip diameter"),
            new("Impeller outlet width"),
            new("Number of impeller blades"),
            new("Impeller inlet length"),
            new("Valve opening coefficient"),

			//AirHumidifer  ( 개수 : 8 )
			new("Air humidifier manifold inlet diameter"),
            new("Air humidifier shell diameter"),
            new("Air humidifier wall thickness"),
            new("Air humidifier manifold length"),
            new("Air humidifier membrane inner diameter"),
            new("Air humidifier membrane thickness"),
            new("Number of humidifier membrane in cathode"),
            new("Air humidifier membrane length"),

			//CleanWater  ( 개수 : 4 )
			new("Fresh water pump efficiency"),
            new("Fresh water pump inlet pressure"),
            new("Fresh water pump outlet pressure"),
            new("Fresh water pump maximum flow rate"),

			//SeaWater  ( 개수 : 5 )
			new("Sea water pump efficiency"),
            new("Sea water pump inlet pressure"),
            new("Sea water pump outlet pressure"),
            new("Sea water pump maximum flow rate"),
            new("Sea water temperature"),

			//HeatExchanger  ( 개수 : 3 )
			new("Heat exchanger active area"),
            new("Heat exchanger overall heat transfer coefficient"),
            new("Heat exchanger temperature setting"),

			//Intercooler  ( 개수 : 4 )
			new("Intercooler inlet coolant mass flow rate"),
            new("Intercooler inlet relative humidity"),
            new("Intercooler inlet coolant temperature"),
            new("Intercooler area"),
            };
        }

        public PortGuideShipModuleViewModel()
        {
            Database = GetDatabaseDefinition();

            //DeleteInVisibleData();
            MainViewModel = App.Container.GetInstance<MainViewModel>();
            Data = new ObservableCollection<ColumnDefinition>();
            OutputData = new ObservableCollection<OutputDefinition>();

            //upload 속도 프로파일
            UploadVelocityCommand = new DelegateCommand(UploadVelocityFile);

            //upload 된 프로파일 삭제
            RemoveTextCommand = new DelegateCommand(RemoveText);

            // 레이아웃 선택
            SelectLayoutCommand = new DelegateCommand(ShowLayoutSelectionDialog);

            // 데이터 시리즈 초기화
            VelocityLineDataSeries = new XyDataSeries<double, double> { SeriesName = "Velocity Profile" };

            // 데이터 시리즈 적용
            SetDefaultVelocity();
            UpdateVelocityLineDataSeries();

            // Chart ViewModels 초기화
            GridViewModel = new PostGridViewModel("Grid Properties", this);
            ChartViewModel = new PostTimeChartViewModel("Time Chart", this);
            XYChartViewModel = new PostChartViewModel("XY Chart", this);
        }


        public PortGuideShipModuleViewModel(string displayName)
        {
            DisplayName = displayName;
            IsClosed = false;
            IsActive = true;
        }

        public void DeleteInVisibleData()
        {

            // Input Data Visible 
            //초기값 설정
            foreach (var Tables in Database.Tables)
            {
                foreach (var Columns in Tables.Columns)
                {
                    Tables.Columns_Visible.Add(Columns);
                }
            }

            ObservableCollection<ColumnDefinition> removeData = new ObservableCollection<ColumnDefinition>();
            foreach (var Tables in Database.Tables)
            {
                foreach (var Columns in Tables.Columns)
                {
                    if (Columns.IsVisible == false)
                    {
                        removeData.Add(Columns);
                    }
                }

                for (int i = 0; i < removeData.Count; i++)
                {
                    Tables.Columns_Visible.Remove(removeData);
                }
                removeData.Clear();
            }

            foreach (var Tables in Database.Tables)
            {
                Tables.VisibleData.Add(Tables.ImageDefinition);
                foreach (var Columns in Tables.Columns_Visible)
                {
                    Tables.VisibleData.Add(Columns);
                }
            }


            //OutputData Visible 

            if (BaseMWModel is PortGuideShipMW PortGuideShipMW)
            {
                foreach (var Tables in Database.Tables)
                {
                    foreach (var outdata in PortGuideShipMW.PortGuideShipMWOuts)
                    {
                        if (Tables.Name.Equals(outdata.Parent))
                        {
                            var OutputDefinition = new OutputDefinition
                            {
                                Name = outdata.Name,
                                IsVisible = false,
                                TableName = Tables.Name,
                                Unit = outdata.Unit,
                            };
                            OutputDefinition.Ob = outdata.SubjectValue.Sample(TimeSpan.FromSeconds(0.2)).Subscribe(x => Application.Current.Dispatcher.BeginInvoke(new Action(delegate { OutputDefinition.Value = x.ToString(); }))); ;
                            Tables.OutputColumns.Add(OutputDefinition);

                        }
                    }
                }
            }
        }

        public void InputShowHideRefresh(ColumnDefinition selData)
        {
            if (selData.IsVisible == true)
            {
                ColumnDefinition RemoveData = new ColumnDefinition();
                foreach (var Tables in Database.Tables)
                {
                    if (Tables.Name.Equals(selData.TableName))
                    {
                        foreach (var Columns in Tables.Columns_Visible)
                        {
                            if (Columns == selData)
                            {
                                RemoveData = Columns;
                            }
                        }
                        if (RemoveData == selData)
                        {
                            Tables.Columns_Visible.Remove(RemoveData);
                            Tables.VisibleData.Remove(RemoveData);
                        }
                    }
                }
            }

            else if (selData.IsVisible == false)
            {
                foreach (var Tables in Database.Tables)
                {
                    if (Tables.Name.Equals(selData.TableName))
                    {
                        Tables.Columns_Visible.Add(selData);
                        Tables.VisibleData.Add(selData);
                    }
                }
            }
            //selData.Init = "Test";
            selData.IsVisible = !selData.IsVisible;
        }


        public void OutputShowHideRefresh(OutputDefinition selData)
        {
            if (selData.IsVisible == true)
            {
                OutputDefinition RemoveData = new OutputDefinition();
                foreach (var Tables in Database.Tables)
                {
                    if (Tables.Name.Equals(selData.TableName))
                    {
                        Tables.VisibleData.Remove(selData);
                    }
                }
            }

            else if (selData.IsVisible == false)
            {
                foreach (var Tables in Database.Tables)
                {
                    if (Tables.Name.Equals(selData.TableName))
                    {
                        Tables.VisibleData.Add(selData);
                    }
                }
            }

            selData.IsVisible = !selData.IsVisible;
        }


        public DatabaseDefinition GetDatabaseDefinition()
        {
            using (var stream = GetDataStream(@"PortGuideShipModel.xml"))
            {
                var serializer = new XmlSerializer(typeof(DatabaseDefinition));
                return (DatabaseDefinition)serializer.Deserialize(stream);
            }
        }

        public Stream GetDataStream(string fileName)
        {

            string path = System.IO.Directory.GetCurrentDirectory() + @"\Models\" + fileName;
            return File.OpenRead(path);
        }

        public void DiagramDoubleClicked(object args)
        {
            //MessageBox.Show(args.GetType().ToString());
            if (args is ReadOnlyCollection<DiagramItem>)
            {
                var selDatas = args as ReadOnlyCollection<DiagramItem>;
                if (selDatas.Count > 0)
                {
                    var seldata = selDatas[0];
                    DiagramItem Items;

                    if (seldata.CustomStyleId.ToString().Equals("columnStyle"))
                    {
                        Items = seldata.ParentItem;
                    }
                    else if (seldata.CustomStyleId.ToString().Equals("ImageStyle"))
                    {
                        Items = seldata.ParentItem;
                    }
                    else
                    {
                        Items = seldata;
                    }

                    TableDefinition ItemsData = (TableDefinition)Items.DataContext;
                    string selTableName = ItemsData.Name;
                    foreach (var Tables in Database.Tables)
                    {
                        if (Tables.Name.Equals(selTableName))
                        {
                            Data.Clear();
                            OutputData.Clear();
                            Data.Add(Tables.Columns);
                            OutputData.Add(Tables.OutputColumns);

                        }
                    }

                    PropertyImagePath = @"/Resource/Detail/" + selTableName + @"Detail.svg";
                    //Data = new DictionaryWrapper<object>(dict);
                }

            }
        }

        public void OnClickCalculateButton()
        {
            if (BaseMWModel is PortGuideShipMW PortGuideShipMW)
            {
                // 일시정지 상태였으면 재개만 하고 리턴
                if (PortGuideShipMW.IsPause == true)
                {
                    Debug.WriteLine("PortGuideShip: 일시정지 상태 -> 재개");

                    // 그래프 재개
                    try
                    {
                        if (ChartViewModel != null)
                        {
                            ChartViewModel.ResetPauseState();
                            Debug.WriteLine("PortGuideShip: TimeChart 재개");
                        }

                        if (XYChartViewModel != null)
                        {
                            XYChartViewModel.ResetPauseState();
                            Debug.WriteLine("PortGuideShip: XYChart 재개");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"PortGuideShip: 그래프 재개 실패: {ex.Message}");
                    }

                    PortGuideShipMW.manualEvent.Set();
                    PortGuideShipMW.IsPause = false;
                    MainViewModel.Status = "Calculation Resumed";
                    return;
                }

                // 이미 실행 중이면 아무것도 하지 않음
                if (PortGuideShipMW.CalculateThread != null && PortGuideShipMW.CalculateThread.IsAlive)
                {
                    Debug.WriteLine("PortGuideShip: 이미 계산 실행 중");
                    MainViewModel.Status = "Calculation already running";
                    return;
                }

                // 새로운 계산 시작
                Debug.WriteLine("PortGuideShip: 새로운 계산 시작");

                // 이전 상태 정리 (CityBus와 동일한 조건)
                if (PortGuideShipMW.IsinitValue == true || (PortGuideShipMW.CalculateThread != null && PortGuideShipMW.CalculateThread.IsAlive))
                {
                    Debug.WriteLine("PortGuideShip: 이전 상태 정리 - StopCalculation() 호출");
                    PortGuideShipMW.StopCalculation();
                }

                // 그래프 완전 초기화
                try
                {
                    if (ChartViewModel != null)
                    {
                        ChartViewModel.ClearChart();
                        Debug.WriteLine("PortGuideShip: TimeChart 초기화 완료");
                    }

                    if (XYChartViewModel != null)
                    {
                        XYChartViewModel.ClearChart();
                        Debug.WriteLine("PortGuideShip: XYChart 초기화 완료");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"PortGuideShip: 그래프 초기화 실패: {ex.Message}");
                }

                if (_uploadedVelocityLines == null || _uploadedVelocityLines.All(value => value == 0))
                {
                    SetDefaultVelocity();
                }
                PortGuideShipMW.SetDriveModes(_uploadedVelocityLines);

                Debug.WriteLine($"PortGuideShip input list length : {PortGuideShipMWInputs.Count}");
                int i = 0;
                foreach (var Table in Database.Tables)
                {
                    foreach (var Column in Table.Columns)
                    {
                        if (i >= PortGuideShipMWInputs.Count)
                        {
                            PortGuideShipMWInputs.Add(new PortGuideShipMWDataModel(Column.Name));
                        }
                        double result = 0;
                        if (Double.TryParse(Column.Init, out result) == false)
                        {
                            break;
                        }

                        double adjustedValue = result;

                        // Min/Max 파싱
                        if (Double.TryParse(Column.Min, out double minVal) && Double.TryParse(Column.Max, out double maxVal))
                        {
                            adjustedValue = GetLayoutAdjustedValue(result, minVal, maxVal, Column.Name);
                        }

                        PortGuideShipMWInputs[i].Value = adjustedValue;
                        i++;
                    }
                }
                PortGuideShipMW.PortGuideShipMWInputs = PortGuideShipMWInputs;

                // ViewModel의 레이아웃 설정을 Model로 전달 (그래프 패턴 변경용)
                PortGuideShipMW.DesignLayout = DesignLayout;
                PortGuideShipMW.ControlLayout = ControlLayout;
                Debug.WriteLine($"PortGuideShip: Layout 설정 - Design={DesignLayout}, Control={ControlLayout}");

                PortGuideShipMW.Calculate();
            }
        }

        public void OnClickPauseButton()
        {
            if (BaseMWModel is PortGuideShipMW PortGuideShipMW)
            {
                if (PortGuideShipMW.CalculateThread != null && PortGuideShipMW.CalculateThread.IsAlive)
                {
                    PortGuideShipMW.manualEvent.Reset();
                    PortGuideShipMW.IsPause = true;
                    MainViewModel.Status = "Calculation Paused";

                    // 그래프 일시정지
                    try
                    {
                        Debug.WriteLine("PortGuideShip: 그래프 일시정지");

                        if (ChartViewModel != null)
                        {
                            ChartViewModel.PauseChart();
                            Debug.WriteLine("PortGuideShip: TimeChart 일시정지");
                        }

                        if (XYChartViewModel != null)
                        {
                            XYChartViewModel.PauseChart();
                            Debug.WriteLine("PortGuideShip: XYChart 일시정지");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"PortGuideShip: 그래프 일시정지 실패: {ex.Message}");
                    }
                }
            }
        }
        public void OnClickStopButton()
        {
            if (BaseMWModel is PortGuideShipMW PortGuideShipMW)
            {
                if (PortGuideShipMW.CalculateThread != null && PortGuideShipMW.CalculateThread.IsAlive)
                {
                    Debug.WriteLine("PortGuideShip: 정지");

                    PortGuideShipMW.StopCalculation();
                    MainViewModel.Status = "Calculation Stopped";

                    // 그래프 완전 초기화
                    try
                    {
                        Debug.WriteLine("PortGuideShip: 그래프 초기화");

                        if (ChartViewModel != null)
                        {
                            ChartViewModel.ClearChart();
                            Debug.WriteLine("PortGuideShip: TimeChart 초기화 완료");
                        }

                        if (XYChartViewModel != null)
                        {
                            XYChartViewModel.ClearChart();
                            Debug.WriteLine("PortGuideShip: XYChart 초기화 완료");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"PortGuideShip: 그래프 초기화 실패: {ex.Message}");
                    }
                }
                else
                {
                    MainViewModel.Status = "No active calculation to stop.";
                }
            }
        }


        public void OnClickNextStepButton()
        {
            //값 입력이 되어있는 상태인지, 아닌지 구분 ( bool) 
            //입력되어있으면 그냥 step 호출, 현재 step 변수를 i가 아닌 전역변수로 설정 
            //안되어 있으면 초기화 후 step 호출 

            if (BaseMWModel is PortGuideShipMW PortGuideShipMW)
            {
                if (PortGuideShipMW.IsPause == true) // 일시정지 상태일 경우에만 실행
                {
                    if (PortGuideShipMW.CalculateThread != null && PortGuideShipMW.CalculateThread.IsAlive)
                    {
                        PortGuideShipMW.OneStep();

                    }

                }
            }

        }

        public void OnClickSaveButton()
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Vehicle Config (*.hr2v)|*.hr2v|All files (*.*)|*.*",
                DefaultExt = ".hr2v",
                FileName = "PortGuideShip_Config",
                InitialDirectory = VehicleSaveData.GetDefaultDir("PortGuideShip")
            };
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var saveData = new VehicleSaveData
                    {
                        VehicleType = "PortGuideShip",
                        DesignLayout = DesignLayout,
                        ControlLayout = ControlLayout,
                        DriveModePath = _lastDriveModePath,
                        DatabaseXml = VehicleSaveData.SerializeDatabase(Database)
                    };
                    saveData.Save(dialog.FileName);
                    VehicleSaveData.RememberDir("PortGuideShip", dialog.FileName);
                    System.Windows.MessageBox.Show("저장 완료", "Save", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"저장 실패: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
        }

        public void OnClickLoadButton()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Vehicle Config (*.hr2v)|*.hr2v|All files (*.*)|*.*",
                DefaultExt = ".hr2v",
                InitialDirectory = VehicleSaveData.GetDefaultDir("PortGuideShip")
            };
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var saveData = VehicleSaveData.Load(dialog.FileName);
                    Database = VehicleSaveData.DeserializeDatabase(saveData.DatabaseXml) ?? Database;
                    DesignLayout = saveData.DesignLayout;
                    ControlLayout = saveData.ControlLayout;
                    if (!string.IsNullOrEmpty(saveData.DriveModePath) && File.Exists(saveData.DriveModePath))
                    {
                        _lastDriveModePath = saveData.DriveModePath;
                        _uploadedVelocityLines = File.ReadAllLines(saveData.DriveModePath)
                            .Select(line =>
                            {
                                var tokens = line.Split(new char[] { '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                                if (tokens.Length > 0 && double.TryParse(tokens[^1], out double value))
                                    return value;
                                return 0.0;
                            })
                            .ToArray();
                        UpdateVelocityLineDataSeries();
                    }
                    UpdateLayoutVisibility();
                    System.Windows.MessageBox.Show("불러오기 완료", "Load", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"불러오기 실패: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
        }

        public void OnExportCsvButton()
        {
            if (BaseMWModel is GenericPortDllModel model && model.HasRecordedData)
            {
                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                    DefaultExt = ".csv",
                    FileName = "PortGuideShip_Result"
                };
                if (dialog.ShowDialog() == true)
                {
                    model.ExportToCsv(dialog.FileName);
                    System.Windows.MessageBox.Show("CSV 저장 완료", "Export", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                }
            }
            else
            {
                System.Windows.MessageBox.Show("저장할 데이터가 없습니다. 시뮬레이션을 먼저 실행하세요.", "Export", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            }
        }

        private void Udp_GridUpdate(object sender, HoRang2Sea.Services.DataReceivedEventArgs e)
        {
            System.Windows.Application.Current?.Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    if (GridViewModel == null || GridViewModel.listsource == null) return;
                    if (BaseMWModel is PortGuideShipMW PortGuideShipMW)
                    {
                        // 노이즈 시간 업데이트
                        UpdateNoiseTime();

                        foreach (var gridItem in GridViewModel.listsource.ToList())
                        {
                            var data = PortGuideShipMW.PortGuideShipMWOuts.FirstOrDefault(d => d.Name == gridItem.Name);
                            if (data != null)
                            {
                                double valueWithNoise = GetOutputWithNoise(data.Value * GetDisplayMultiplier());
                                gridItem.UpdateInternal(valueWithNoise);
                            }
                        }
                    }
                }
                catch (Exception ex) { Debug.WriteLine($"Udp_GridUpdate 오류: {ex.Message}"); }
            }));
        }

        private void Udp_ChartUpdate(object sender, HoRang2Sea.Services.DataReceivedEventArgs e)
        {
            System.Windows.Application.Current?.Dispatcher.BeginInvoke(new Action(() => { ChartUpdate(); }));
        }

        public void ChartUpdate()
        {
            try
            {
                ChartViewModel?.ChartUpdate();
                XYChartViewModel?.ChartUpdate();
            }
            catch (Exception ex) { Debug.WriteLine($"ChartUpdate 오류: {ex.Message}"); }
        }

        public void ShowLayoutSelectionDialog()
        {
            var dialog = new System.Windows.Window
            {
                Title = "Select Layout - PortGuideShip",
                Width = 700, Height = 680,
                WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen,
                ResizeMode = System.Windows.ResizeMode.NoResize,
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(245, 247, 250))
            };

            var mainGrid = new System.Windows.Controls.Grid { Margin = new System.Windows.Thickness(35) };
            var headerStack = new System.Windows.Controls.StackPanel { Margin = new System.Windows.Thickness(0, 0, 0, 25) };
            headerStack.Children.Add(new System.Windows.Controls.TextBlock
            {
                Text = "Layout Configuration", FontSize = 26, FontWeight = System.Windows.FontWeights.Bold,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center
            });

            var buttonGrid = new System.Windows.Controls.Grid { HorizontalAlignment = System.Windows.HorizontalAlignment.Center };
            buttonGrid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition());
            buttonGrid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition());
            buttonGrid.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition());
            buttonGrid.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition());

            System.Windows.Controls.Button CreateBtn(string text, string desc, int dv, int cv)
            {
                var btn = new System.Windows.Controls.Button { Width = 280, Height = 150, Margin = new System.Windows.Thickness(12), Background = System.Windows.Media.Brushes.White };
                var stack = new System.Windows.Controls.StackPanel { HorizontalAlignment = System.Windows.HorizontalAlignment.Center, VerticalAlignment = System.Windows.VerticalAlignment.Center };
                stack.Children.Add(new System.Windows.Controls.TextBlock { Text = text, FontSize = 18, FontWeight = System.Windows.FontWeights.Bold, HorizontalAlignment = System.Windows.HorizontalAlignment.Center });
                stack.Children.Add(new System.Windows.Controls.TextBlock { Text = desc, FontSize = 12, HorizontalAlignment = System.Windows.HorizontalAlignment.Center });
                btn.Content = stack;
                btn.Click += (s, e) =>
                {
                    // 시뮬레이션 실행 중이거나 일시정지 상태면 먼저 정지
                    if (BaseMWModel is PortGuideShipMW mw && mw.CalculateThread != null && (mw.CalculateThread.IsAlive || mw.IsPause))
                    {
                        mw.StopCalculation();
                    }
                    DesignLayout = dv; ControlLayout = cv; UpdateLayoutVisibility(); dialog.Close();
                };
                return btn;
            }

            var btn1 = CreateBtn("Default Layout", "Design: 0 | Control: 0", 0, 0); System.Windows.Controls.Grid.SetRow(btn1, 0); System.Windows.Controls.Grid.SetColumn(btn1, 0);
            var btn2 = CreateBtn("Control Mode", "Design: 0 | Control: 1", 0, 1); System.Windows.Controls.Grid.SetRow(btn2, 0); System.Windows.Controls.Grid.SetColumn(btn2, 1);
            var btn3 = CreateBtn("Design Mode", "Design: 1 | Control: 0", 1, 0); System.Windows.Controls.Grid.SetRow(btn3, 1); System.Windows.Controls.Grid.SetColumn(btn3, 0);
            var btn4 = CreateBtn("Full Configuration", "Design: 1 | Control: 1", 1, 1); System.Windows.Controls.Grid.SetRow(btn4, 1); System.Windows.Controls.Grid.SetColumn(btn4, 1);

            buttonGrid.Children.Add(btn1); buttonGrid.Children.Add(btn2); buttonGrid.Children.Add(btn3); buttonGrid.Children.Add(btn4);

            var container = new System.Windows.Controls.StackPanel();
            container.Children.Add(headerStack); container.Children.Add(buttonGrid);
            mainGrid.Children.Add(container);
            dialog.Content = mainGrid;
            dialog.ShowDialog();
        }

    }
}
