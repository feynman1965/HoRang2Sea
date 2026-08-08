using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DevExpress.Mvvm.POCO;
using DevExpress.Utils.About;
using DevExpress.Xpf;
using DevExpress.Xpf.Core.Native;
using DevExpress.Xpf.DataAccess.DataSourceWizard.Native;
using HoRang2Sea.Common;
using HoRang2Sea.Services;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Input;

namespace HoRang2Sea.ViewModels
{
    public class ProjectViewModel
    {
        public ProjectViewModel(string name, string image)
        {
            Name = name;
            Image = image;
        }
        public string Name { get; set; }
        public string Image { get; set; }
    }
    public class RecentItemViewModel
    {
        static int count = 0;
        public string Caption { get; set; }
        public string Description { get; set; }
        public DateTime DateModified { get; set; }
        public RecentItemViewModel() { }
        public RecentItemViewModel(string caption, string description)
        {
            Caption = caption;
            Description = description;
            DateModified = DateTime.Now.AddDays(count);
            count++;
        }
    }

    // Home "Saved Configs" 한 항목 (사용자가 저장한 .hr2v 1개)
    public class SavedConfigEntry
    {
        public string Title { get; set; }        // 사용자가 정한 파일명(확장자 제외) = 목록 제목
        public string VehicleType { get; set; }  // SolutionType 이름 (예: "FishingBoat")
        public string FilePath { get; set; }
        public string SavedDate { get; set; }
    }

    // Home "Saved Configs" 모델 종류별 그룹
    public class SavedConfigGroupViewModel
    {
        public string TypeName { get; set; }
        public ObservableCollection<SavedConfigEntry> Items { get; set; }
    }

    public class MainViewModel : ViewModelBase
    {
        public ISplashScreenService SplashScreenService { get { return this.GetService<ISplashScreenService>(); } }


        const string filter = "Configuration (*.HR2)|*.hr2|All files (*.*)|*.*";
        public int Id { get; set; }
        CommandViewModel Connect;
        CommandViewModel DisConnect;
        CommandViewModel newFile;
        CommandViewModel openFile;
        CommandViewModel openProject;
        CommandViewModel save;
        CommandViewModel startView;
        CommandViewModel output;
        //CommandViewModel property;
        CommandViewModel solutionExplorer;
        //CommandViewModel monitor;
        PanelWorkspaceViewModel lastOpenedItem;
        ObservableCollection<WorkspaceViewModel> workspaces;
        public RecentItemViewModel[] RecentDocuments { get; protected set; }
        public List<ProjectViewModel> ProjectViewModels { get; protected set; }

        public ICommand OpenSavedConfigCommand { get; private set; }
        public ICommand DeleteConfigCommand { get; private set; }
        public ICommand LoadProjectCommand { get; private set; }

        // Home 백스테이지에 표시할 "저장된 모델 설정(.hr2v)" 목록 (모델 종류별 그룹)
        private ObservableCollection<SavedConfigGroupViewModel> savedConfigs;
        public ObservableCollection<SavedConfigGroupViewModel> SavedConfigs
        {
            get => savedConfigs;
            set { savedConfigs = value; RaisePropertyChanged(nameof(SavedConfigs)); }
        }

        // 백스테이지 "History" 탭: Run 시 자동 저장되는 최근 스냅샷(_history, 최대 20개/모델, 종류별 그룹)
        private ObservableCollection<SavedConfigGroupViewModel> historyConfigs;
        public ObservableCollection<SavedConfigGroupViewModel> HistoryConfigs
        {
            get => historyConfigs;
            set { historyConfigs = value; RaisePropertyChanged(nameof(HistoryConfigs)); }
        }

        // 백스테이지 "Settings" 탭: 결과 기록 간격 (step). 1 step = 1 ms.
        private static readonly int[] RecordIntervalChoices = { 10, 100, 500, 1000 };

        public int RecordIntervalIndex
        {
            get
            {
                int cur = Models.AppSettings.Current.RecordStepInterval;
                int idx = Array.IndexOf(RecordIntervalChoices, cur);
                return idx < 0 ? 1 : idx;   // 목록에 없는 값이면 기본(0.1초) 위치를 보여준다
            }
            set
            {
                if (value < 0 || value >= RecordIntervalChoices.Length) return;
                var s = Models.AppSettings.Current;
                if (s.RecordStepInterval == RecordIntervalChoices[value]) return;
                s.RecordStepInterval = RecordIntervalChoices[value];
                s.Save();
                RaisePropertyChanged(nameof(RecordIntervalIndex));
                RaisePropertyChanged(nameof(RecordIntervalHint));
            }
        }

        /// <summary>선택한 간격이 메모리를 얼마나 쓰는지 대략 알려준다(가장 무거운 모델 기준).</summary>
        public string RecordIntervalHint
        {
            get
            {
                int iv = Models.AppSettings.Current.RecordStepInterval;
                if (iv < 1) iv = 1;
                // 가장 무거운 축(출력 210개, 200만 step)을 기준으로 한 어림치
                double mb = 2_000_000.0 / iv * (210 + 1) * 8 / 1024 / 1024;
                return $"Rough memory use for the heaviest model (2,000,000 steps, 210 outputs): about {mb:N0} MB per run.";
            }
        }

        public MainViewModel()
        {
            SplashScreenService.ShowSplashScreen();

            OutputViewModel = App.Container.GetInstance<OutputViewModel>();
            SolutionExplorerViewModel = CreatePanelWorkspaceViewModel<SolutionExplorerViewModel>();
            SolutionExplorerViewModel.ItemOpening += SolutionExplorerViewModel_ItemOpening;
            StartViewModel = CreatePanelWorkspaceViewModel<StartViewModel>();
            Bars = new ReadOnlyCollection<BarModel>(CreateBars());
            // 더미 하드코딩 경로(c:\My Documents\*.rtf) 제거 — 실제 최근 문서가 없으면 빈 목록.
            RecentDocuments = new RecentItemViewModel[] { };
            LoadProjectCommand = new DelegateCommand(LoadProject);
            OpenSavedConfigCommand = new DelegateCommand<SavedConfigEntry>(OpenSavedConfig);
            DeleteConfigCommand = new DelegateCommand<SavedConfigEntry>(DeleteConfig);
            BackstageOpenedCommand = new DelegateCommand(RefreshAllConfigs);
            SavedConfigs = new ObservableCollection<SavedConfigGroupViewModel>();
            HistoryConfigs = new ObservableCollection<SavedConfigGroupViewModel>();
            RefreshAllConfigs();
            ProjectViewModels = new List<ProjectViewModel>()
            {
                /*new("Nexo","/Resource/Vehiclesflat/3-suv.svg"),*/
                new("Fishing Boat","/Resource/Vehiclesflat/56-FishingBoat.svg"),
                new("Port Guide Ship","/Resource/Vehiclesflat/57-PortGuideShip.svg"),
                new("Training Ship","/Resource/Vehiclesflat/55-TrainingShip.svg"),
                
                /*new("Courier truck","/Resource/Vehiclesflat/10-van.svg"),
                new("Small Tactical","/Resource/Vehiclesflat/25-hoverboard.svg"),
                new("Golf car","/Resource/Vehiclesflat/47-golf-car.svg"),
                new("Subminiature","/Resource/Vehiclesflat/48-hatchback.svg"),  
                new("Road sweeper","/Resource/Vehiclesflat/17-grader.svg"),
                new("Airport car","/Resource/Vehiclesflat/43-pickup.svg"),
                new("Rail(general)","/Resource/Vehiclesflat/7-tram.svg"),
                new("Rail(city)","/Resource/Vehiclesflat/8-train.svg"),
                new("Excavators","/Resource/Vehiclesflat/15-excavator.svg"),*/
            };
            InitDefaultLayout();

        }

        public ReadOnlyCollection<BarModel> Bars { get; private set; }
        public OutputViewModel OutputViewModel { get; private set; }
        public SolutionExplorerViewModel SolutionExplorerViewModel { get; private set; }
        public StartViewModel StartViewModel { get; private set; }

        public virtual IBackstageViewService BackstageViewService { get { return null; } }

        public ICommand BackstageOpenedCommand { get; private set; }

        //public PropertyViewModel PropertyViewModel { get; private set; }
        //public MonitorViewModel MonitorViewModel { get; private set; }
        public ObservableCollection<WorkspaceViewModel> Workspaces
        {
            get
            {
                if (workspaces == null)
                {
                    workspaces = new ObservableCollection<WorkspaceViewModel>();
                    workspaces.CollectionChanged += OnWorkspacesChanged;
                }
                return workspaces;
            }
        }

        public string Status
        {
            get { return GetValue<string>(); }
            set
            {
                if (SetValue(value))
                    RaisePropertyChanged("Status");
            }
        }

        // 앱 타이틀바 텍스트: 기본 "HoRang2 Sea", 모델 열면 "HoRang2 Sea — {모델명}".
        public string WindowTitle
        {
            get { return GetValue<string>() ?? "HoRang2 Sea"; }
            set
            {
                if (SetValue(value))
                    RaisePropertyChanged("WindowTitle");
            }
        }



        protected virtual ISaveLoadLayoutService SaveLoadLayoutService { get { return null; } }

        protected T CreatePanelWorkspaceViewModel<T>() where T : PanelWorkspaceViewModel
        {
            return ViewModelSource<T>.Create();
        }

        protected virtual List<CommandViewModel> CreateViewCommands()
        {
            output = GetShowCommand(OutputViewModel);
            startView = GetShowCommand(StartViewModel);
            solutionExplorer = GetShowCommand(SolutionExplorerViewModel);
            return new List<CommandViewModel>() {
                startView,output,solutionExplorer/*,property,monitor*/
            };
        }

        public void OpenOrCloseWorkspace(PanelWorkspaceViewModel workspace, bool activateOnOpen = true)
        {
            if (Workspaces.Contains(workspace))
            {
                workspace.IsClosed = !workspace.IsClosed;
            }
            else
            {
                Workspaces.Add(workspace);
                workspace.IsClosed = false;
            }
            if (activateOnOpen && workspace.IsOpened)
                SetActiveWorkspace(workspace);
        }
        bool ActivateDocument(string path)
        {
            var document = GetDocument(path);
            bool isFound = document != null;
            if (isFound) document.IsActive = true;
            return isFound;
        }
        List<BarModel> CreateBars()
        {
            return new List<BarModel>() {
                new BarModel("Main") { IsMainMenu = true, Commands = CreateCommands() },
                new BarModel("Standard") { Commands = CreateToolbarCommands() }
            };
        }
        List<CommandViewModel> CreateCommands()
        {
            return new List<CommandViewModel> {
                new CommandViewModel("File", CreateFileCommands()),
                new CommandViewModel("View", CreateViewCommands()),
            };
        }
        DocumentViewModel CreateDocumentViewModel()
        {
            return CreatePanelWorkspaceViewModel<DocumentViewModel>();
        }

        List<CommandViewModel> CreateFileCommands()
        {
            var fileExecutedCommand = new DelegateCommand<object>(OnFileOpenExecuted);
            var fileOpenCommand = new DelegateCommand<object>(OnFileOpenExecuted);

            CommandViewModel newCommand = new CommandViewModel("New") { IsSubItem = true };

            newFile = new CommandViewModel("New File", fileExecutedCommand) { Glyph = CommonFunction.GetGlyphFromUri("pack://application:,,,/DevExpress.Images.v25.1;component/SvgImages/Dashboards/New.svg")/*Images.File*/, KeyGesture = new KeyGesture(Key.N, ModifierKeys.Control) };
            newCommand.Commands = new List<CommandViewModel>() { newFile };

            CommandViewModel openCommand = new CommandViewModel("Open") { IsSubItem = true, };
            openProject = new CommandViewModel("Project/Solution...", new DelegateCommand(LoadProject))
            {
                Glyph = CommonFunction.GetGlyphFromUri("pack://application:,,,/DevExpress.Images.v25.1;component/SvgImages/Actions/Open2.svg")/*Images.OpenSolution*/,
                IsEnabled = true,
                KeyGesture = new KeyGesture(Key.O, ModifierKeys.Control | ModifierKeys.Shift),
            };
            openFile = new CommandViewModel("Open File", fileOpenCommand)
            {
                Glyph = CommonFunction.GetGlyphFromUri("pack://application:,,,/DevExpress.Images.v25.1;component/SvgImages/Actions/Open.svg")/*Images.OpenFile*/,
                KeyGesture = new KeyGesture(Key.O, ModifierKeys.Control)
            };
            openCommand.Commands = new List<CommandViewModel>() { openProject/*, openFile*/ };

            CommandViewModel closeFile = new CommandViewModel("Close");
            CommandViewModel closeSolution = new CommandViewModel("Close Solution") { Glyph = CommonFunction.GetGlyphFromUri("pack://application:,,,/DevExpress.Images.v25.1;component/SvgImages/XAF/Action_CloseAllWindows.svg")/*Images.CloseSolution*/ };
            save = new CommandViewModel("Save", new DelegateCommand(SaveProject)) { Glyph = CommonFunction.GetGlyphFromUri("pack://application:,,,/DevExpress.Images.v25.1;component/SvgImages/DiagramIcons/save.svg"), KeyGesture = new KeyGesture(Key.S, ModifierKeys.Control) };

            return new List<CommandViewModel>() { newCommand, openCommand, GetSeparator(), closeFile, closeSolution, GetSeparator(), save };
        }
        List<CommandViewModel> CreateToolbarCommands()
        {
            return new List<CommandViewModel>() {
                 newFile, /*openFile,*/ save, openProject, /*GetSeparator(),  Connect, DisConnect,*/
                GetSeparator(), startView, output, solutionExplorer, /*property monitor*/
            };
        }

        private void ConnetStatusChanged(object sender, EventArgs e)
        {
            //DisConnect.IsEnabled = MonitorViewModel.ConnetStatus;
            //Connect.IsEnabled = !MonitorViewModel.ConnetStatus;
        }

        DocumentViewModel GetDocument(string filePath)
        {
            return Workspaces.OfType<DocumentViewModel>().FirstOrDefault(x => x.DisplayName == filePath);
        }
        CommandViewModel GetSeparator()
        {
            return new CommandViewModel() { IsSeparator = true };
        }
        public CommandViewModel GetShowCommand(PanelWorkspaceViewModel viewModel)
        {
            return new CommandViewModel(viewModel, new DelegateCommand(() => OpenOrCloseWorkspace(viewModel)));
        }
        void InitDefaultLayout()
        {
            var panels = new List<PanelWorkspaceViewModel> { StartViewModel };
            foreach (var panel in panels)
            {
                OpenOrCloseWorkspace(panel, false);
            }
        }

        void OnFileOpenExecuted(object param)
        {
            var document = CreateDocumentViewModel();
            if (!document.OpenFile() || ActivateDocument(document.FilePath))
            {
                document.Dispose();
                return;
            }
            OpenOrCloseWorkspace(document);
        }
        // "Save Current Model" — 현재 활성 모델의 설정(.hr2v)을 저장한다(모델별 Save Config 다이얼로그).
        public void SaveProject()
        {
            var doc = Workspaces.OfType<DocumentViewModel>().FirstOrDefault(d => d.IsActive)
                      ?? Workspaces.OfType<DocumentViewModel>().LastOrDefault();
            if (doc == null)
            {
                MessageBox.Show("Open a vehicle model first, then save its configuration.", "Save Model", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            BackstageViewService?.Close();
            doc.SaveConfig();          // 각 모델 VM의 Save Config 다이얼로그 (파일명=목록 제목)
            RefreshAllConfigs();        // 방금 저장한 항목을 목록에 반영
        }

        // Home "Saved Configs" 목록 항목 클릭 → 해당 모델을 열고 config를 적용한다.
        private void OpenSavedConfig(SavedConfigEntry entry)
        {
            if (entry == null || !File.Exists(entry.FilePath)) return;
            if (!Enum.TryParse<SolutionType>(entry.VehicleType, out var type)) return;

            var item = SolutionItem.Create(entry.Title, "", type);
            OpenItem(item, showLayoutDialog: false);   // config가 레이아웃을 담고 있으므로 선택 팝업은 생략

            // 방금 열린(또는 동일 제목으로 이미 열려 활성화된) 문서에 config 적용
            var doc = Workspaces.OfType<DocumentViewModel>().FirstOrDefault(d => d.IsActive);
            doc?.LoadConfig(entry.FilePath);
            BackstageViewService?.Close();
        }

        // 저장 폴더를 스캔해 종류별 그룹 목록 생성. history=false → 수동 저장(.hr2v), true → _history 스냅샷.
        private ObservableCollection<SavedConfigGroupViewModel> BuildConfigGroups(bool history)
        {
            var groups = new ObservableCollection<SavedConfigGroupViewModel>();
            try
            {
                var root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HoRang2", "Configs");
                foreach (SolutionType type in Enum.GetValues(typeof(SolutionType)))
                {
                    if (type == SolutionType.GRID || type == SolutionType.XYCHART) continue;
                    var dir = history
                        ? Path.Combine(root, type.ToString(), "_history")
                        : Path.Combine(root, type.ToString());
                    if (!Directory.Exists(dir)) continue;
                    var files = Directory.GetFiles(dir, "*.hr2v");
                    if (files.Length == 0) continue;

                    var entries = new ObservableCollection<SavedConfigEntry>();
                    foreach (var f in files.OrderByDescending(f => File.GetLastWriteTime(f)))
                    {
                        entries.Add(new SavedConfigEntry
                        {
                            Title = Path.GetFileNameWithoutExtension(f),
                            VehicleType = type.ToString(),
                            FilePath = f,
                            SavedDate = File.GetLastWriteTime(f).ToString("yyyy-MM-dd HH:mm")
                        });
                    }
                    groups.Add(new SavedConfigGroupViewModel { TypeName = GetVehicleDisplayName(type), Items = entries });
                }
            }
            catch { }
            return groups;
        }

        public void RefreshSavedConfigs() => SavedConfigs = BuildConfigGroups(history: false);
        public void RefreshHistoryConfigs() => HistoryConfigs = BuildConfigGroups(history: true);
        public void RefreshAllConfigs() { RefreshSavedConfigs(); RefreshHistoryConfigs(); }

        // Saved/History 목록의 ×(삭제) — 해당 .hr2v 파일 삭제 후 목록 갱신.
        private void DeleteConfig(SavedConfigEntry entry)
        {
            if (entry == null || string.IsNullOrEmpty(entry.FilePath)) return;
            var r = MessageBox.Show($"Delete this configuration?\n\n{entry.Title}", "Delete", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (r != MessageBoxResult.Yes) return;
            try { if (File.Exists(entry.FilePath)) File.Delete(entry.FilePath); }
            catch (Exception ex) { MessageBox.Show($"Delete failed: {ex.Message}", "Delete", MessageBoxButton.OK, MessageBoxImage.Error); }
            RefreshAllConfigs();
        }

        private static string GetVehicleDisplayName(SolutionType type)
        {
            switch (type)
            {
                case SolutionType.FishingBoat: return "Fishing Boat";
                case SolutionType.PortGuideShip: return "Port Guide Ship";
                case SolutionType.TrainingShip: return "Training Ship";
                default: return type.ToString();
            }
        }

        public void LoadProject()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog() { Filter = filter };
            var openResult = openFileDialog.ShowDialog();
            if (openResult.HasValue && openResult.Value)
            {
                string filename = openFileDialog.FileName;

                var solution = App.Container.GetInstance<Solution>();
                solution.clear();
                solution.LoadFile(filename);
                solution.Items.Where(item => !item.mymodel.IsClosed).ForEach(solutionitem => OpenItem(solutionitem));



                SaveLoadLayoutService.LoadLayout(Path.Combine(Path.GetDirectoryName(filename), Path.GetFileNameWithoutExtension(filename) + ".xml"));
            }
        }
        void OnLoadLayout()
        {
            SaveLoadLayoutService.LoadLayout();
        }


        /*void OnNewFileExecuted(object param)
        {
            var solution = App.Container.GetInstance<Solution>();

            solution.NewItem();
        }*/
        void OnSaveLayout()
        {
            SaveLoadLayoutService.SaveLayout();
        }
        void OnWorkspaceRequestClose(object sender, EventArgs e)
        {
            var workspace = sender as PanelWorkspaceViewModel;
            if (workspace != null)
            {
                workspace.IsClosed = true;
                if (workspace is DocumentViewModel)
                {
                    workspace.Dispose();
                    Workspaces.Remove(workspace);
                }
            }
        }
        void OnWorkspacesChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null && e.NewItems.Count != 0)
                foreach (WorkspaceViewModel workspace in e.NewItems)
                    workspace.RequestClose += OnWorkspaceRequestClose;
            if (e.OldItems != null && e.OldItems.Count != 0)
                foreach (WorkspaceViewModel workspace in e.OldItems)
                    workspace.RequestClose -= OnWorkspaceRequestClose;
        }
        void OpenItem(string filePath)
        {
            if (ActivateDocument(filePath)) return;
            lastOpenedItem = CreateDocumentViewModel();
            lastOpenedItem.OpenItemByPath(filePath);
            OpenOrCloseWorkspace(lastOpenedItem);
        }
        void OpenItem(SolutionItem item, bool showLayoutDialog = true)
        {
            WindowTitle = "HoRang2 Sea";
            if (ActivateDocument(item.Name)) return;
            if (item.Workspace == null)
            {
                switch (item.Type)
                {
                    case SolutionType.GRID:
                        lastOpenedItem = CreatePanelWorkspaceViewModel<PostViewModel>();
                        break;

                    case SolutionType.FishingBoat:
                        lastOpenedItem = CreatePanelWorkspaceViewModel<FishingBoatModuleViewModel>();
                        if (showLayoutDialog && lastOpenedItem is FishingBoatModuleViewModel fishingBoatVm)
                        {
                            System.Windows.Application.Current?.Dispatcher.BeginInvoke(new System.Action(() =>
                            {
                                fishingBoatVm.ShowLayoutSelectionDialog();
                            }), System.Windows.Threading.DispatcherPriority.Loaded);
                        }
                        break;
                    case SolutionType.PortGuideShip:
                        lastOpenedItem = CreatePanelWorkspaceViewModel<PortGuideShipModuleViewModel>();
                        if (showLayoutDialog && lastOpenedItem is PortGuideShipModuleViewModel portGuideShipVm)
                        {
                            System.Windows.Application.Current?.Dispatcher.BeginInvoke(new System.Action(() =>
                            {
                                portGuideShipVm.ShowLayoutSelectionDialog();
                            }), System.Windows.Threading.DispatcherPriority.Loaded);
                        }
                        break;
                    case SolutionType.TrainingShip:
                        lastOpenedItem = CreatePanelWorkspaceViewModel<TrainingShipModuleViewModel>();
                        if (showLayoutDialog && lastOpenedItem is TrainingShipModuleViewModel trainingShipVm)
                        {
                            System.Windows.Application.Current?.Dispatcher.BeginInvoke(new System.Action(() =>
                            {
                                trainingShipVm.ShowLayoutSelectionDialog();
                            }), System.Windows.Threading.DispatcherPriority.Loaded);
                        }
                        break;

                    default:
                        return;
                }
                lastOpenedItem.OpenItemByItem(item);
                //PropertiesViewModel.SelectedItem = new PropertyItem(lastOpenedItem);
            }
            else
                lastOpenedItem = item.Workspace;

            OpenOrCloseWorkspace(lastOpenedItem);
        }
        void SetActiveWorkspace(WorkspaceViewModel workspace)
        {
            workspace.IsActive = true;
        }
        void ShowAbout()
        {
            About.ShowAbout(ProductKind.DXperienceWPF);
        }
        void SolutionExplorerViewModel_ItemOpening(object sender, SolutionItemOpeningEventArgs e)
        {
            OpenItem(e.SolutionItem);

            //OpenItem(e.SolutionItem.FilePath);
        }

        public void CreatDocument(object Name)
        {
            var Selname = Name.ToString();

            switch (Selname)
            {

                case "Fishing Boat":
                    CreateFishingBoatDocument(Selname);
                    break;
                case "Port Guide Ship":
                    CreatePortGuideShipDocument(Selname);
                    break;
                case "Training Ship":
                    CreateTrainingSHipDocument(Selname);
                    break;

                default:
                    return;



            }
        }


        public async void CreateFishingBoatDocument(string Name)
        {
            var solution = App.Container.GetInstance<Solution>();
            var newitem = solution.NewFishingBoatProject();
            OpenItem(newitem);
            BackstageViewService?.Close();
        }
        public async void CreatePortGuideShipDocument(string Name)
        {
            var solution = App.Container.GetInstance<Solution>();
            var newitem = solution.NewPortGuideShipProject();
            OpenItem(newitem);
            BackstageViewService?.Close();
        }

        public async void CreateTrainingSHipDocument(string Name)
        {
            var solution = App.Container.GetInstance<Solution>();
            var newitem = solution.NewTrainingShipProject();
            OpenItem(newitem);
            BackstageViewService?.Close();
        }



    }
}
