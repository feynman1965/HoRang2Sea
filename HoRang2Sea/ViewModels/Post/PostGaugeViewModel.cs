using DevExpress.Map.Native;
using DevExpress.Mvvm;
using HoRang2Sea.Models;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Windows;

namespace HoRang2Sea.ViewModels
{
    public class PostGaugeViewModel : PostPanelWorkspaceViewModel
    {
        public PostGaugeViewModel(string displayName, PostViewModel parent)
        {
            DisplayName = displayName;
            IsClosed = false;
            IsActive = true;
            ParentViewModel = parent;
            GaugeGlobalItems = new ObservableCollection<string>();
            GaugeItems = new ObservableCollection<string>();
            OnGaugePropertyCommand = new DelegateCommand(OnGaugeProperty);
            StartValue = 0;
            EndValue = 100;
        }
        protected override string WorkspaceName { get { return "BottomHost"; } }
        public PostViewModel ParentViewModel { get; set; }
        public ObservableCollection<string> GaugeGlobalItems { get; set; }
        public ObservableCollection<string> GaugeItems { get; set; }
        public DelegateCommand OnGaugePropertyCommand { get; set; }


        public int StartValue
        {
            get { return GetValue<int>(); }
            set
            {
                if (SetValue(value))
                    RaisePropertyChanged("StartValue");
            }
        }

        public int EndValue
        {
            get { return GetValue<int>(); }
            set
            {
                if (SetValue(value))
                    RaisePropertyChanged("EndValue");
            }
        }

        public IDisposable Ob;

        public double Value
        {
            get { return GetValue<double>(); }
            set
            {
                if (SetValue(value))
                    RaisePropertyChanged("Value");
            }
        }

        public string Name
        {
            get { return GetValue<string>(); }
            set
            {
                if (SetValue(value))
                    RaisePropertyChanged("Name");
            }
        }

        public string Unit
        {
            get { return GetValue<string>(); }
            set
            {
                if (SetValue(value))
                    RaisePropertyChanged("Unit");
            }
        }



        public void OnGaugeProperty()
        {
            if (ParentViewModel.BaseMWModel is FishingBoatMW FishingBoatMW)
            {
                GaugeGlobalItems = new ObservableCollection<string>
                    (FishingBoatMW.FishingBoatMWOuts.Where(mw => !GaugeItems.Any(i => i == mw.Name)).Select(wd => wd.Name).ToList());
            }
            else if (ParentViewModel.BaseMWModel is PortGuideShipMW PortGuideShipMW)
            {
                GaugeGlobalItems = new ObservableCollection<string>
                    (PortGuideShipMW.PortGuideShipMWOuts.Where(mw => !GaugeItems.Any(i => i == mw.Name)).Select(wd => wd.Name).ToList());
            }
            else if (ParentViewModel.BaseMWModel is TrainingShipMW TrainingShipMW)
            {
                GaugeGlobalItems = new ObservableCollection<string>
                    (TrainingShipMW.TrainingShipMWOuts.Where(mw => !GaugeItems.Any(i => i == mw.Name)).Select(wd => wd.Name).ToList());
            }
 




            UICommand rOkCommand = new UICommand()
            {
                Caption = "Ok",
                IsDefault = true,
                Command = new DelegateCommand(() =>
                {
                    try
                    {
                        GaugeSet();
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
            IDialogService service = this.GetService<IDialogService>("ChartPropertyDialogService");
            UICommand result = service.ShowDialog(
                dialogCommands: new[] { rOkCommand, rCloseCommand },
                title: "Import Data",
                viewModel: this
            );
        }



        public void GaugeSet()
        {

            if (ParentViewModel.BaseMWModel is FishingBoatMW FishingBoatMW)
            {
                foreach (var Gaugeitem in GaugeItems)
                {
                    var data = FishingBoatMW.FishingBoatMWOuts.FirstOrDefault(d => d.Name == Gaugeitem);
                    if (data != null)
                    {
                        Name = data.Name;
                        Unit = data.Unit;
                        if (Ob is not null)
                            Ob.Dispose();
                        Ob = data.SubjectValue.Sample(TimeSpan.FromSeconds(0.2)).Subscribe(x => Application.Current.Dispatcher.BeginInvoke(new Action(delegate { Value = x; }))); ;
                    }
                }
            }
            else if (ParentViewModel.BaseMWModel is PortGuideShipMW PortGuideShipMW)
            {
                foreach (var Gaugeitem in GaugeItems)
                {
                    var data = PortGuideShipMW.PortGuideShipMWOuts.FirstOrDefault(d => d.Name == Gaugeitem);
                    if (data != null)
                    {
                        Name = data.Name;
                        Unit = data.Unit;
                        if (Ob is not null)
                            Ob.Dispose();
                        Ob = data.SubjectValue.Sample(TimeSpan.FromSeconds(0.2)).Subscribe(x => Application.Current.Dispatcher.BeginInvoke(new Action(delegate { Value = x; }))); ;
                    }
                }
            }
            
            else if (ParentViewModel.BaseMWModel is TrainingShipMW TrainingShipMW)
            {
                foreach (var Gaugeitem in GaugeItems)
                {
                    var data = TrainingShipMW.TrainingShipMWOuts.FirstOrDefault(d => d.Name == Gaugeitem);
                    if (data != null)
                    {
                        Name = data.Name;
                        Unit = data.Unit;
                        if (Ob is not null)
                            Ob.Dispose();
                        Ob = data.SubjectValue.Sample(TimeSpan.FromSeconds(0.2)).Subscribe(x => Application.Current.Dispatcher.BeginInvoke(new Action(delegate { Value = x; }))); ;
                    }
                }
            }


        }


        public void DataUpdate()
        {
            //try
            //{
            //    if (ParentViewModel.NexoMWModel is NexoMW nexoMW)
            //    {
            //        foreach (var Gaugeitem in GaugeItems)
            //        {
            //            var data = nexoMW.NexoMWOuts.FirstOrDefault(d => d.Name == Gaugeitem);
            //            if (data != null)
            //            {
            //                Value = data.Value;
            //            }
            //        }
            //    }
            //}

            //catch (Exception ex)
            //{
            //}
        }

    }
}
