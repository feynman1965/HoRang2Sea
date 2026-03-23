using DevExpress.Mvvm;
using DevExpress.Mvvm.POCO;
using DevExpress.Pdf.Native.BouncyCastle.Utilities.IO;
using DevExpress.Utils.Serializing;
using DevExpress.XtraCharts;
using HoRang2Sea.Models;
using HoRang2Sea.ViewModels;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace HoRang2Sea
{
    public class Solution
    {
        private int FishingBoatID = 1;
        private int PortGuideShipID = 1;
        private int TrainingShipID = 1;
        private int Con14tonID = 1;



        protected Solution()
        {
            Items = new ObservableCollection<SolutionItem>();  //타입추가하기.
        }

        public ObservableCollection<SolutionItem> Items { get; private set; }

        public SolutionItem NewFishingBoatProject()
        {
            string ItemName = string.Format("FishingBoat Project {0}", FishingBoatID++);
            while (Items.Any(item => item.Name == ItemName))
            {
                ItemName = string.Format("FishingBoat Project {0}", FishingBoatID++);
            }
            SolutionItem item = SolutionItem.Create(ItemName, "pack://application:,,,/DevExpress.Images.v22.2;component/SvgImages/Dashboards/InsertTreeView.svg", SolutionType.FishingBoat);
            Items.Add(item);  //타입추가하기.

            SolutionItem Griditem = SolutionItem.Create(ItemName + "_Monitor", "pack://application:,,,/DevExpress.Images.v22.2;component/SvgImages/Icon Builder/Actions_Table.svg", SolutionType.GRID);
            Griditem.mymodel.BaseMWModel = item.mymodel.BaseMWModel = new FishingBoatMW();
            item.Items.Add(Griditem);
            return item;
        }
        public SolutionItem NewPortGuideShipProject()
        {
            string ItemName = string.Format("PortGuideShip Project {0}", PortGuideShipID++);
            while (Items.Any(item => item.Name == ItemName))
            {
                ItemName = string.Format("PortGuideShip Project {0}", PortGuideShipID++);
            }
            SolutionItem item = SolutionItem.Create(ItemName, "pack://application:,,,/DevExpress.Images.v22.2;component/SvgImages/Dashboards/InsertTreeView.svg", SolutionType.PortGuideShip);
            Items.Add(item);  //타입추가하기.

            SolutionItem Griditem = SolutionItem.Create(ItemName + "_Monitor", "pack://application:,,,/DevExpress.Images.v22.2;component/SvgImages/Icon Builder/Actions_Table.svg", SolutionType.GRID);

            Griditem.mymodel.BaseMWModel = item.mymodel.BaseMWModel = new PortGuideShipMW();
            item.Items.Add(Griditem);
            return item;
        }
        public SolutionItem NewTrainingShipProject()
        {
            string ItemName = string.Format("TrainingShip Project {0}", TrainingShipID++);
            while (Items.Any(item => item.Name == ItemName))
            {
                ItemName = string.Format("TrainingShip Project {0}", TrainingShipID++);
            }
            SolutionItem item = SolutionItem.Create(ItemName, "pack://application:,,,/DevExpress.Images.v22.2;component/SvgImages/Dashboards/InsertTreeView.svg", SolutionType.TrainingShip);
            Items.Add(item);  //타입추가하기.

            SolutionItem Griditem = SolutionItem.Create(ItemName + "_Monitor", "pack://application:,,,/DevExpress.Images.v22.2;component/SvgImages/Icon Builder/Actions_Table.svg", SolutionType.GRID);

            Griditem.mymodel.BaseMWModel = item.mymodel.BaseMWModel = new TrainingShipMW();
            item.Items.Add(Griditem);
            return item;
        }
       




        /* public void NewItem()
         {
             string ItemName = string.Format("New Item {0}", NewID++);
             while (Items.Any(item => item.Name == ItemName))
             {
                 ItemName = string.Format("New Item {0}", NewID++);
             }
             SolutionItem item = SolutionItem.Create(ItemName, "pack://application:,,,/DevExpress.Images.v22.2;component/SvgImages/Icon Builder/Actions_Table.svg", SolutionType.GRID);
             Items.Add(item);  //타입추가하기.
         }*/
        public void Save()
        {
            SaveFileDialog saveFD = new()
            {
                Filter = "XML Files (*.xml)|*.xml",
                FilterIndex = 1
            };
            bool? dialogResult = saveFD.ShowDialog();
            bool dialogResultOK = dialogResult.HasValue && dialogResult.Value;
            if (dialogResultOK)
            {
                var pocoTypes = new Type[] { ViewModelSource.GetPOCOType(typeof(SolutionItem)) };

                using (ExcludingPOCOTypesXmlTextWriter xmlWriter = new ExcludingPOCOTypesXmlTextWriter(saveFD.FileName, pocoTypes))
                {
                    xmlWriter.Formatting = Formatting.Indented;

                    XmlSerializer ser = new XmlSerializer(typeof(ObservableCollection<SolutionItem>), pocoTypes);
                    ser.Serialize(xmlWriter, Items);
                }
            }
        }
        public void SaveFile(string filename)
        {

            var pocoTypes = new Type[] { ViewModelSource.GetPOCOType(typeof(SolutionItem)) };

            using (ExcludingPOCOTypesXmlTextWriter xmlWriter = new ExcludingPOCOTypesXmlTextWriter(filename, pocoTypes))
            {
                xmlWriter.Formatting = Formatting.Indented;

                XmlSerializer ser = new XmlSerializer(typeof(ObservableCollection<SolutionItem>), pocoTypes);
                ser.Serialize(xmlWriter, Items);
            }
        }

        public string SaveString()
        {
            string ret = "";

            var pocoTypes = new Type[] { ViewModelSource.GetPOCOType(typeof(SolutionItem)) };
            using (MemoryStream SolutionStram = new MemoryStream())
            using (ExcludingPOCOTypesXmlTextWriter xmlWriter = new ExcludingPOCOTypesXmlTextWriter(SolutionStram, pocoTypes))
            {
                xmlWriter.Formatting = Formatting.Indented;
                XmlSerializer ser = new XmlSerializer(typeof(ObservableCollection<SolutionItem>), pocoTypes);
                ser.Serialize(xmlWriter, Items);

                ret = Encoding.UTF8.GetString(SolutionStram.GetBuffer(), 0, (int)SolutionStram.Length);
                //StreamReader reader = new StreamReader(SolutionStram);

                //ret = reader.ReadToEnd();
            }
            return ret;
        }

        public void clear()
        {
            Items.Clear();
        }

        public void Load()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "XML Files (*.xml)|*.xml";
            openFileDialog.FilterIndex = 1;
            bool? dialogResult = openFileDialog.ShowDialog();
            bool dialogResultOK = dialogResult.HasValue && dialogResult.Value;
            if (dialogResultOK)
            {
                using (StreamReader fs = new StreamReader(openFileDialog.FileName))
                {
                    XmlSerializer ser = new XmlSerializer(typeof(ObservableCollection<SolutionItem>));
                    var deres = (ObservableCollection<SolutionItem>)ser.Deserialize(fs);

                    foreach (var de in deres)
                    {
                        de.PostRead();
                        if (de.mymodel != null)
                            de.mymodel.PostRead();
                        Items.Add(SolutionItem.Create(de));
                    }
                }
            }
        }
        public void LoadFile(string filename)
        {

            using (StreamReader fs = new StreamReader(filename))
            {
                XmlSerializer ser = new XmlSerializer(typeof(ObservableCollection<SolutionItem>));
                var deres = (ObservableCollection<SolutionItem>)ser.Deserialize(fs);

                foreach (var de in deres)
                {
                    de.PostRead();
                    if (de.mymodel != null)
                        de.mymodel.PostRead();
                    Items.Add(SolutionItem.Create(de));
                }
            }
        }
        public void LoadString(string load)
        {
            byte[] byteArray = Encoding.UTF8.GetBytes(load);
            using (MemoryStream fs = new MemoryStream(byteArray))
            {
                XmlSerializer ser = new XmlSerializer(typeof(ObservableCollection<SolutionItem>));
                var deres = (ObservableCollection<SolutionItem>)ser.Deserialize(fs);

                foreach (var de in deres)
                {
                    if (de.mymodel != null)
                        de.mymodel.PostRead();
                    Items.Add(SolutionItem.Create(de));
                }
            }
        }
    }

    public class ExcludingPOCOTypesXmlTextWriter : XmlTextWriter
    {
        IEnumerable<Type> _pocoTypes;
        bool isTypeAttributeWriting;
        public ExcludingPOCOTypesXmlTextWriter(TextWriter w) : base(w) { }
        public ExcludingPOCOTypesXmlTextWriter(Stream w, Encoding encoding) : base(w, encoding) { }
        public ExcludingPOCOTypesXmlTextWriter(Stream w, IEnumerable<Type> pocoTypes) : base(w, Encoding.UTF8)
        {
            _pocoTypes = pocoTypes;
        }
        public ExcludingPOCOTypesXmlTextWriter(string filename, Encoding encoding) : base(filename, encoding) { }
        public ExcludingPOCOTypesXmlTextWriter(string filename, IEnumerable<Type> pocoTypes)
            : base(filename, Encoding.UTF8)
        {
            _pocoTypes = pocoTypes;
        }
        public override void WriteStartAttribute(string prefix, string localName, string ns)
        {
            isTypeAttributeWriting = localName == "type";
            base.WriteStartAttribute(prefix, localName, ns);
        }
        public override void WriteString(string text)
        {
            if (isTypeAttributeWriting)
            {
                isTypeAttributeWriting = false;
                var excludedMatchType = _pocoTypes.FirstOrDefault((t) => t.Name == text);
                if (excludedMatchType != null)
                {
                    base.WriteString(excludedMatchType.BaseType.Name);
                    return;
                }
            }
            base.WriteString(text);
        }
    }
}
