using DevExpress.Data;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using HoRang2Sea.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HoRang2Sea.ViewModels
{
    public class PostGridViewModel : PostPanelWorkspaceViewModel
    {
        private bool _isPropertyDialogOpen;

        public PostGridViewModel(string displayName, DocumentViewModel parent = null)
        {
            DisplayName = displayName;
            IsClosed = false;
            IsActive = true;
            OnUseRealTimeSource();

            GridItems = new ObservableCollection<string>();
            GridGlobalItems = new ObservableCollection<string>();
            OnGridPropertyCommand = new DelegateCommand(OnGridProperty);
            ParentViewModel = parent;

            if (_isPropertyDialogOpen) return;
            GridItems.CollectionChanged += GridItems_CollectionChanged;
        }

        private void GridItems_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (_isPropertyDialogOpen) return;
            UpdateGridGlobalItems();
            UpdateFilteredList();
        }

        protected override string WorkspaceName { get { return "TopHost"; } }

        public virtual object Source { get; set; }
        public ObservableCollection<GridData> listsource;

        public ObservableCollection<string> GridItems { get; set; }
        public ObservableCollection<string> GridGlobalItems { get; set; }

        public DelegateCommand OnGridPropertyCommand { get; set; }
        public DocumentViewModel ParentViewModel { get; set; }

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
                return null;
            }
        }

        private string _searchKeyword;
        private ObservableCollection<string> _filteredGridGlobalItems;

        public string SearchKeyword
        {
            get => _searchKeyword;
            set
            {
                SetValue(ref _searchKeyword, value);
                UpdateFilteredList();
            }
        }

        public ObservableCollection<string> FilteredGridGlobalItems
        {
            get => _filteredGridGlobalItems;
            set => SetValue(ref _filteredGridGlobalItems, value);
        }

        private void UpdateFilteredList()
        {
            if (FilteredGridGlobalItems == null)
            {
                FilteredGridGlobalItems = new ObservableCollection<string>();
            }

            FilteredGridGlobalItems.Clear();

            if (string.IsNullOrEmpty(SearchKeyword))
            {
                foreach (var item in GridGlobalItems)
                {
                    FilteredGridGlobalItems.Add(item);
                }
            }
            else
            {
                var filtered = GridGlobalItems
                    .Where(item => item != null && item.IndexOf(SearchKeyword, StringComparison.OrdinalIgnoreCase) >= 0);

                foreach (var item in filtered)
                {
                    FilteredGridGlobalItems.Add(item);
                }
            }

            RaisePropertyChanged(nameof(FilteredGridGlobalItems));
        }

        private void UpdateGridGlobalItems()
        {
            if (_isPropertyDialogOpen) return;
            List<string> newItems = null;

            if (BaseMWModel is FishingBoatMW FishingBoatMW)
            {
                newItems = FishingBoatMW.FishingBoatMWOuts
                    .Where(mw => !GridItems.Contains(mw.Name))
                    .Select(wd => wd.Name)
                    .ToList();
            }
            else if (BaseMWModel is PortGuideShipMW PortGuideShipMW)
            {
                newItems = PortGuideShipMW.PortGuideShipMWOuts
                    .Where(mw => !GridItems.Contains(mw.Name))
                    .Select(wd => wd.Name)
                    .ToList();
            }
            else if (BaseMWModel is TrainingShipMW TrainingShipMW)
            {
                newItems = TrainingShipMW.TrainingShipMWOuts
                    .Where(mw => !GridItems.Contains(mw.Name))
                    .Select(wd => wd.Name)
                    .ToList();
            }

            if (newItems != null)
            {
                GridGlobalItems.Clear();
                foreach (var item in newItems)
                {
                    GridGlobalItems.Add(item);
                }
            }
        }

        public void OnGridProperty()
        {
            _isPropertyDialogOpen = true;
            try
            {
                List<string> newItems = null;

                if (BaseMWModel is FishingBoatMW FishingBoatMW)
                {
                    newItems = FishingBoatMW.FishingBoatMWOuts
                        .Where(mw => !GridItems.Any(i => i == mw.Name))
                        .Select(wd => wd.Name)
                        .ToList();
                }
                else if (BaseMWModel is PortGuideShipMW PortGuideShipMW)
                {
                    newItems = PortGuideShipMW.PortGuideShipMWOuts
                        .Where(mw => !GridItems.Any(i => i == mw.Name))
                        .Select(wd => wd.Name)
                        .ToList();
                }
                else if (BaseMWModel is TrainingShipMW TrainingShipMW)
                {
                    newItems = TrainingShipMW.TrainingShipMWOuts
                        .Where(mw => !GridItems.Any(i => i == mw.Name))
                        .Select(wd => wd.Name)
                        .ToList();
                }

                if (newItems != null)
                {
                    GridGlobalItems.Clear();
                    foreach (var item in newItems)
                    {
                        GridGlobalItems.Add(item);
                    }
                }

                UpdateFilteredList();

                if (FilteredGridGlobalItems == null || FilteredGridGlobalItems.Count == 0)
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
                            GridSet();
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"GridSet Error: {ex.Message}");
                        }
                    })
                };

                UICommand rCloseCommand = new UICommand()
                {
                    Caption = "Close",
                    IsCancel = true
                };

                IDialogService service = this.GetService<IDialogService>("GridPropertyDialogService");
                UICommand result = service.ShowDialog(
                    dialogCommands: new[] { rOkCommand, rCloseCommand },
                    title: "Import Data",
                    viewModel: this
                );
            }
            finally
            {
                _isPropertyDialogOpen = false;
                UpdateGridGlobalItems();
                UpdateFilteredList();
            }
        }

        public void GridSet()
        {
            if (System.Windows.Application.Current?.Dispatcher.CheckAccess() == false)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() => GridSet());
                return;
            }

            listsource.Clear();

            foreach (var gridItem in GridItems.Distinct())
            {
                string unit = "";
                if (BaseMWModel is FishingBoatMW FishingBoatMW)
                {
                    var dataItem = FishingBoatMW.FishingBoatMWOuts.FirstOrDefault(d => d.Name == gridItem);
                    if (dataItem != null)
                    {
                        unit = dataItem.Unit;
                    }
                }
                else if (BaseMWModel is PortGuideShipMW PortGuideShipMW)
                {
                    var dataItem = PortGuideShipMW.PortGuideShipMWOuts.FirstOrDefault(d => d.Name == gridItem);
                    if (dataItem != null)
                    {
                        unit = dataItem.Unit;
                    }
                }
                else if (BaseMWModel is TrainingShipMW TrainingShipMW)
                {
                    var dataItem = TrainingShipMW.TrainingShipMWOuts.FirstOrDefault(d => d.Name == gridItem);
                    if (dataItem != null)
                    {
                        unit = dataItem.Unit;
                    }
                }

                listsource.Add(new GridData(gridItem, "", 0, unit));
            }
        }

        protected void OnUseRealTimeSource()
        {
            listsource = new ObservableCollection<GridData>();
            Source = new RealTimeSource { DataSource = listsource };
        }

    }


}
