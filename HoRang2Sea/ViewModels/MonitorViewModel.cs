using DevExpress.Mvvm.DataAnnotations;
using HoRang2Sea.Models;
using HoRang2Sea.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Data;
using System.Windows.Forms;

namespace HoRang2Sea.ViewModels
{
    public class MonitorViewModel : PanelWorkspaceViewModel
    {
        protected override string WorkspaceName { get { return "Toolbox"; } }
        public UdpService udpService;
        private JsonDataParser parser = new JsonDataParser();
        private object _lock = new object();
        private bool isUpdated = false;
        private List<double> refreshRates = new List<double>();
        private System.Diagnostics.Stopwatch stopWatch = new System.Diagnostics.Stopwatch();

        #region constructor
        public MonitorViewModel()
        {
            DisplayName = "Monitor";
            Glyph = Common.CommonFunction.GetGlyphFromUri("pack://application:,,,/DevExpress.Images.v22.2;component/SvgImages/Icon Builder/Electronics_DesktopWindows.svg");
            BindingOperations.EnableCollectionSynchronization(DataModels, _lock);
            ReadSetup("TaskMonitor.setting.json");
            ConnetStatus = false;
        }

        #endregion
        #region property
        public int Port { get; set; }
        public int UpdateInterval
        { get { return GetValue<int>(); } set { SetValue(value < 100 ? 100 : value); } }
        public double UpdateIntervalXY
        { get { return GetValue<double>(); } set { SetValue(value < 1 ? 1 : value); } }
        public int DataLength
        { get { return GetValue<int>(); } set { SetValue(value); } }
        public string RefreshRate
        { get { return GetValue<string>(); } set { SetValue(value); } }

        public event EventHandler RequestConnetStatusChanged;

        [BindableProperty(OnPropertyChangedMethodName = "OnConnetStatusChanged")]
        public virtual bool ConnetStatus
        { get { return GetValue<bool>(); } set { SetValue(value); } }

        public ObservableCollection<DataModel> DataModels { get; set; } = new ObservableCollection<DataModel>();

        #endregion
        #region method
        public virtual void OnConnetStatusChanged()
        {
            EventHandler handler = RequestConnetStatusChanged;
            if (handler != null)
                handler(this, EventArgs.Empty);
        }
        private void ReadSetup(string file)
        {
            if (File.Exists(file))
            {
                using (StreamReader reader = new StreamReader(file))
                {
                    var setup = JsonSerializer.Deserialize<MonitorSetupModel>(reader.ReadToEnd());
                    if (setup != null)
                    {
                        Port = setup.DefaultPort;
                        UpdateInterval = setup.UpdateInterval;
                        UpdateIntervalXY = setup.UpdateIntervalXY;
                    }
                }
            }
        }
        public void ConnectMethod()

        {
            if (!ConnetStatus)
            {
                udpService = new UdpService(Port);
                udpService.DataReceived += Udp_DataReceived;
                ConnetStatus = true;
                StartCalculateRefreshRate();
            }
            else
            {
                udpService.DataReceived -= Udp_DataReceived;
                udpService.Disconnect();
                ConnetStatus = false;
                StopCalculateRefreshRate();
                DataModels.Clear();
            }
        }
        private void Udp_DataReceived(object sender, DataReceivedEventArgs e)
        {
            try
            {
                parser.ParseData(e.Data, good => good.ForEach(g => UpdateData(g)));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            if (isUpdated)
            {
                refreshRates.Add(CalculateRefreshRate());
                while (refreshRates.Count > 10)
                    refreshRates.RemoveAt(0);
                RefreshRate = $"{refreshRates.Average():F1} Hz";
                isUpdated = false;
            }

            void UpdateData(DataModel selectedData)
            {
                if (DataModels.Count(c => c.FullName == selectedData.FullName) > 0)
                {
                    DataModel dataToChange = DataModels.Single(c => c.FullName == selectedData.FullName);
                    dataToChange.Update(selectedData);
                }
                else
                {
                    DataModels.Add(selectedData);
                    var sorted = DataModels.OrderBy(x => x.Module).ThenBy(y => y.ID).ToList();
                    for (int i = 0; i < sorted.Count(); i++)
                        DataModels.Move(DataModels.IndexOf(sorted[i]), i);
                }
                isUpdated = true;
            }
            double CalculateRefreshRate()
            {
                stopWatch.Stop();
                double ms = Convert.ToDouble(stopWatch.Elapsed.TotalMilliseconds);
                double rate = 0;
                if (ms > 0)
                    rate = 1000.0 / ms;
                stopWatch.Restart();
                return rate;
            }
        }
        private void StartCalculateRefreshRate()
        {
            stopWatch = new System.Diagnostics.Stopwatch();
            stopWatch.Start();
        }
        private void StopCalculateRefreshRate()
        {
            RefreshRate = String.Empty;
            refreshRates.Clear();
            stopWatch.Stop();
        }
        #endregion
    }
}
