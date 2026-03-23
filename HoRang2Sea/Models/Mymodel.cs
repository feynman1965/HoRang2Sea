using DevExpress.Mvvm.POCO;
using DevExpress.XtraEditors.Controls;
using HoRang2Sea.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Xml.Serialization;

namespace HoRang2Sea.Models
{
    [XmlInclude(typeof(GridModel))]
    [XmlInclude(typeof(ChartModel))]

    [XmlInclude(typeof(FishingBoatModel))]
    [XmlInclude(typeof(PortGuideShipModel))]
    [XmlInclude(typeof(TrainingShipModel))]


    public abstract class Mymodel
    {
        public bool IsClosed { get; set; }
        public string name { get; set; }
        public string FilePath { get; set; }
        public virtual void PostRead() { }
        public BaseModel BaseMWModel { get; set; }
    }
    public class GridModel : Mymodel
    {
        public List<string> Items { get; set; } = new();
    }
    public class ChartModel : Mymodel
    {
    }

    public class FishingBoatModel : Mymodel
    {
    }
    public class PortGuideShipModel : Mymodel
    {
    }
    public class TrainingShipModel : Mymodel
    {
    }


}