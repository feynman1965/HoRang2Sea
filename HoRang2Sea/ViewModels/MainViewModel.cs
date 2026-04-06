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
        public MainViewModel()
        {
            SplashScreenService.ShowSplashScreen();

            OutputViewModel = App.Container.GetInstance<OutputViewModel>();
            SolutionExplorerViewModel = CreatePanelWorkspaceViewModel<SolutionExplorerViewModel>();
            SolutionExplorerViewModel.ItemOpening += SolutionExplorerViewModel_ItemOpening;
            StartViewModel = CreatePanelWorkspaceViewModel<StartViewModel>();
            Bars = new ReadOnlyCollection<BarModel>(CreateBars());
            RecentDocuments = new RecentItemViewModel[] {
                new RecentItemViewModel("Recent Document 1", @"c:\My Documents\Recent Document 1.rtf"),
                new RecentItemViewModel("Recent Document 2", @"c:\My Documents\Recent Document 2.rtf"),
                new RecentItemViewModel("Recent Document 3", @"c:\My Documents\Recent Document 3.rtf"),
                
            };
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
        public void SaveProject()
        {
            SaveFileDialog dialog = new SaveFileDialog() { Filter = filter };
            dialog.ShowDialog();

            if (!string.IsNullOrEmpty(dialog.FileName))
            {
                string filename = dialog.FileName;
                //solution
                var solution = App.Container.GetInstance<Solution>();
                //
                foreach (var DVM in Workspaces.OfType<DocumentViewModel>())
                {
                    DVM.UpdateToModel();
                }
                solution.SaveFile(filename);
                SaveLoadLayoutService.SaveLayout(Path.Combine(Path.GetDirectoryName(filename), Path.GetFileNameWithoutExtension(filename) + ".xml"));
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
        void OpenItem(SolutionItem item)
        {
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
                        break;
                    case SolutionType.PortGuideShip:
                        lastOpenedItem = CreatePanelWorkspaceViewModel<PortGuideShipModuleViewModel>();
                        break;
                    case SolutionType.TrainingShip:
                        lastOpenedItem = CreatePanelWorkspaceViewModel<TrainingShipModuleViewModel>();
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



                /*case "14ton Construction":
                   CreateCon14tonDocument(Selname);
                   break;*/




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
