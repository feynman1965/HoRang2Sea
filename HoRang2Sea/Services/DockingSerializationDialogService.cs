using DevExpress.Mvvm.UI;
using DevExpress.Utils.Serializing;
using DevExpress.Xpf.Docking;
using DevExpress.Xpf.Grid;
using Microsoft.Win32;
using System;
using System.IO;
using System.Text;

namespace HoRang2Sea.Services
{
    public interface ISaveLoadLayoutService
    {
        void SaveLayout();
        void SaveLayout(string filename);
        void LoadLayout();
        void LoadLayout(string filename);
        string SaveLayoutString();
        void LoadLayoutString(string layout);
    }
    public class DockingSerializationDialogService : ServiceBase, ISaveLoadLayoutService
    {
        const string filter = "Configuration (*.xml)|*.xml|All files (*.*)|*.*";
        public DockLayoutManager DockLayoutManager { get; set; }
        public void LoadLayout()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog() { Filter = filter };
            var openResult = openFileDialog.ShowDialog();
            if (openResult.HasValue && openResult.Value)
                DockLayoutManager.RestoreLayoutFromXml(openFileDialog.FileName);
        }

        public void LoadLayout(string filename)
        {
            DockLayoutManager.RestoreLayoutFromXml(filename);
        }

        public void LoadLayoutString(string layout)
        {
            byte[] byteArray = Encoding.UTF8.GetBytes(layout);
            using (MemoryStream fs = new MemoryStream(byteArray))
            {
                DockLayoutManager.RestoreLayoutFromStream(fs);
            }
        }
        public void SaveLayout()
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog() { Filter = filter };
            var saveResult = saveFileDialog.ShowDialog();
            if (saveResult.HasValue && saveResult.Value)
                DockLayoutManager.SaveLayoutToXml(saveFileDialog.FileName);
        }

        public void SaveLayout(string filename)
        {
            DockLayoutManager.SaveLayoutToXml(filename);
        }

        public string SaveLayoutString()
        {
            string ret = "";
            Stream SolutionStram = new MemoryStream();
            DockLayoutManager.SaveLayoutToStream(SolutionStram);
            StreamReader reader = new StreamReader(SolutionStram);
            ret = reader.ReadToEnd();
            return ret;
        }
        protected override void OnAttached()
        {
            base.OnAttached();
            DockLayoutManager = AssociatedObject as DockLayoutManager;
        }
        protected override void OnDetaching()
        {
            base.OnDetaching();
            DockLayoutManager = null;
        }
    }
    public class GridSerializationService : ServiceBase, ISaveLoadLayoutService
    {
        const string filter = "Configuration (*.xml)|*.xml|All files (*.*)|*.*";
        public GridControl GridControl { get; set; }
        public void LoadLayout()
        {
            throw new NotImplementedException();
        }

        public void LoadLayout(string filename)
        {
            GridControl.RestoreLayoutFromXml(filename);
        }

        public void LoadLayoutString(string layout)
        {
            throw new NotImplementedException();
        }

        public void SaveLayout()
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog() { Filter = filter };
            var saveResult = saveFileDialog.ShowDialog();
            if (saveResult.HasValue && saveResult.Value)
                GridControl.SaveLayoutToXml(saveFileDialog.FileName);
        }

        public void SaveLayout(string filename)
        {
            GridControl.SaveLayoutToXml(filename);
        }

        public string SaveLayoutString()
        {
            throw new NotImplementedException();
        }
        protected override void OnAttached()
        {
            base.OnAttached();
            GridControl = AssociatedObject as GridControl;
        }
        protected override void OnDetaching()
        {
            base.OnDetaching();
            GridControl = null;
        }
    }
}
